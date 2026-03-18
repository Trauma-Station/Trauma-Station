// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tag;
using Content.Trauma.Common.Kitchen;
using Content.Trauma.Shared.DeepFryer;
using Content.Trauma.Shared.DeepFryer.Components;
using Content.Trauma.Shared.DeepFryer.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.DeepFryer;

public sealed class DeepFryerSystem : SharedDeepFryerSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private EntityQuery<TagComponent> _tagQuery;

    private static readonly ProtoId<DamageTypePrototype> damageType = "Heat";

    /// <summary>
    /// Every recipe, updated whenever they reload.
    /// </summary>
    public List<DeepFryerRecipePrototype> AllRecipes = new();

    private Dictionary<ProtoId<TagPrototype>, List<EntityUid>> _ingredients = new();
    // used for all batches of a single recipe
    private HashSet<EntityUid> _consumable = new();
    // a subset of consumable, used for a single batch
    private HashSet<EntityUid> _consumed = new();
    // like _consumed but for reagents and their quantity. there is no _consumable analogue.
    private Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> _reagents = new();

    public override void Initialize()
    {
        base.Initialize();

        _tagQuery = GetEntityQuery<TagComponent>();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        LoadRecipes();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveDeepFryerComponent, DeepFryerComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var active, out var comp))
        {
            var ent = (uid, comp);
            if (!_solution.TryGetSolution(uid, comp.FryerSolutionContainer, out var solution, out _))
            {
                Log.Error($"Deep fryer {ToPrettyString(uid)} has no solution {comp.FryerSolutionContainer}!");
                RemCompDeferred(uid, active);
                continue;
            }

            // TODO: dont use fucking frametime
            AddHeatToSolution(ent, frameTime, comp.HeatToAddToSolution, solution.Value);

            if (comp.StoredObjects.Count == 0)
                continue;

            AddHeatDamage(ent, frameTime);

            if (now >= comp.FryFinishTime && comp.FryFinishTime != TimeSpan.Zero)
            {
                TryCookContents(ent, solution.Value);
                DeepFryItems(ent);
            }
        }
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<DeepFryerRecipePrototype>())
            LoadRecipes();
    }

    private void LoadRecipes()
    {
        AllRecipes.Clear();

        foreach (var proto in _proto.EnumeratePrototypes<DeepFryerRecipePrototype>())
        {
            AllRecipes.Add(proto);
        }
    }

    private void AddHeatToSolution(Entity<DeepFryerComponent> ent, float frameTime, float heatToAdd, Entity<SolutionComponent> solution)
    {
        _solution.AddThermalEnergyClamped(solution, heatToAdd * frameTime, 293f, ent.Comp.MaxHeat);
    }

    private void AddHeatDamage(Entity<DeepFryerComponent> ent, float frameTime)
    {
        var heatProto = _proto.Index(damageType);

        foreach (var entity in ent.Comp.StoredObjects)
        {
            if (!TryComp<DamageableComponent>(entity, out _))
                continue;

            _damageable.TryChangeDamage(entity, new DamageSpecifier(heatProto, ent.Comp.HeatDamage * frameTime));
        }
    }

    private void TryCookContents(Entity<DeepFryerComponent> ent, Entity<SolutionComponent> solution)
    {
        // reset old state, but keep the lists there so they don't get reallocated every time
        foreach (var items in _ingredients.Values)
        {
            items.Clear();
        }

        // collect the tags and items which can satisfy recipes
        foreach (var item in ent.Comp.StoredObjects)
        {
            if (!_tagQuery.TryComp(item, out var tags))
                continue;

            foreach (var tag in tags.Tags)
            {
                if (_ingredients.TryGetValue(tag, out var items))
                    items.Add(item);
                else
                    _ingredients[tag] = [ item ];
            }
        }

        // now check every recipe
        foreach (var recipe in AllRecipes)
        {
            // keep cooking this recipe if there are multiple batches of ingredients present
            var cooked = 0;
            while (TryCookRecipe(ent, recipe, solution))
            {
                cooked++;
            }

            // reset any consumable ingredients that weren't actually consumed on the last recipe in the batch
            _consumable.RemoveWhere(unused => !_consumed.Contains(unused));

            if (cooked > 0 && ent.Comp.LastUser is {} user)
            {
                // let other systems know the good news
                var ev = new CookedFoodEvent(user, recipe.Result, cooked);
                RaiseLocalEvent(user, ref ev);
            }
        }
    }

    private bool TryCookRecipe(Entity<DeepFryerComponent> ent, DeepFryerRecipePrototype recipe, Entity<SolutionComponent> solution)
    {
        _consumed.Clear();
        _reagents.Clear();

        // 1. need every required item in the fryer
        foreach (var (tag, amount) in recipe.Items)
        {
            // not just using the ingredients list length incase a single item has multiple tags
            // have to check each one
            for (int i = 0; i < amount; i++)
            {
                if (FindIngredient(tag) is not {} item)
                    return false; // missing item ingredient, can't continue

                _consumable.Add(item);
                _consumed.Add(item);
            }
        }

        // 2. need every required reagent if they're specified
        foreach (var cost in recipe.Reagents)
        {
            if (!FindReagent(solution.Comp.Solution, cost))
                return false; // missing required reagent, can't continue
        }

        // 3. all requirements were met, delete everything we used in the recipe
        foreach (var item in _consumed)
        {
            Del(item);
        }
        ent.Comp.StoredObjects.RemoveAll(uid => _consumed.Contains(uid));

        var sol = solution.Comp.Solution;
        foreach (var (reagent, quantity) in _reagents)
        {
            sol.RemoveReagent(reagent, quantity, ignoreReagentData: true);
        }
        _solution.UpdateChemicals(solution);

        // 4. spawn the result in the fryer
        if (!TrySpawnInContainer(recipe.Result, ent, SharedEntityStorageSystem.ContainerName, out var result))
        {
            Log.Error($"Failed to spawn result {recipe.Result} of recipe {recipe.ID} into {ToPrettyString(ent)}!");
            return false;
        }

        // store it so it gets deep fried when we're done
        // it doesn't update _ingredients so you can't instantly chain recipes in 1 go
        ent.Comp.StoredObjects.Add(result.Value);
        return true;
    }

    private EntityUid? FindIngredient(ProtoId<TagPrototype> tag)
    {
        if (!_ingredients.TryGetValue(tag, out var items) || items.Count == 0)
            return null; // none in the fryer at all

        // there are some items with that tag in the fryer, check if there's an unused one to consume
        foreach (var item in items)
        {
            if (!_consumable.Contains(item) && !_consumed.Contains(item))
                return item;
        }

        return null; // everything was already consumed for this batch
    }

    private bool FindReagent(Solution solution, ReagentCost cost)
    {
        var needed = cost.Quantity;
        foreach (var reagent in cost.Allowed)
        {
            var found = solution.GetReagentQuantity(new(reagent, null));
            if (found == FixedPoint2.Zero)
                continue; // check other allowed reagents

            // if there's more than enough clamp to what's needed
            if (found >= needed)
                found = needed;

            // store it as being consumed so it can be removed later
            _reagents[reagent] = _reagents.GetValueOrDefault(reagent) + found;

            // if we need more from other allowed reagents, take away what we got from this one
            needed -= found;

            if (needed <= FixedPoint2.Zero)
                return true; // found all of the needed reagent!
        }

        // not enough of it was found. _reagents wont be used since TryCookRecipe
        // will return immediately so it doesn't need to be tracked before getting cleared again
        return false;
    }
}

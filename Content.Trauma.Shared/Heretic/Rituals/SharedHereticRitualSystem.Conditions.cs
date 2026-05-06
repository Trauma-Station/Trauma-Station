// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Text;
using Content.Shared.Stacks;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.Side;

namespace Content.Trauma.Shared.Heretic.Rituals;

public abstract partial class SharedHereticRitualSystem
{
    public void SubscribeConditions()
    {
        SubscribeLocalEvent<TransformComponent, HereticRitualConditionEvent<IsTargetCondition>>(OnTargetCheck);
        SubscribeLocalEvent<TransformComponent, HereticRitualConditionEvent<ConditionsRitualCondition>>(
            OnApplyConditions);
        SubscribeLocalEvent<TransformComponent, HereticRitualConditionEvent<IsLimitedOutputCondition>>(
            OnLimitedCondition);
        SubscribeLocalEvent<HereticRitualComponent, HereticRitualConditionEvent<ProcessIngredientsCondition>>(
            OnProcessIngredients);
        SubscribeLocalEvent<HereticComponent, HereticRitualConditionEvent<CanAscendCondition>>(OnCanAscend);
        SubscribeLocalEvent<HereticComponent, HereticRitualConditionEvent<ObjectivesCompleteCondition>>(
            OnObjectivesComplete);
        SubscribeLocalEvent<HereticRitualComponent, HereticRitualConditionEvent<TryApplyEffectSequenceCondition>>(
            OnApplySequence);

        SubscribeLocalEvent<TransformComponent, HereticRitualConditionEvent<HereticMinStageCondition>>(OnMinStage);
        SubscribeLocalEvent<TransformComponent, HereticRitualConditionEvent<HereticMinPassiveLevelCondition>>(
            OnMinLevel);
        SubscribeLocalEvent<TransformComponent, HereticRitualConditionEvent<BackstabCondition>>(OnBackstab);
        SubscribeLocalEvent<TransformComponent, HereticRitualConditionEvent<TryMakeRustWallCondition>>(OnRustWall);
        SubscribeLocalEvent<TransformComponent, HereticRitualConditionEvent<FleshGhoulLimitCondition>>(
            OnFleshGhoulLimitCheck);
    }

    private void OnFleshGhoulLimitCheck(Entity<TransformComponent> ent,
        ref HereticRitualConditionEvent<FleshGhoulLimitCondition> args)
    {
        if (!TryGetValue(args.Ritual, Performer, out EntityUid user) ||
            !TryGetValue(args.Ritual, Mind, out EntityUid mind) ||
            !TryComp(mind, out FleshHereticMindComponent? fleshMind))
            return;

        fleshMind.Ghouls = fleshMind.Ghouls.Where(Exists).ToList();
        args.Result = fleshMind.Ghouls.Count < fleshMind.GhoulLimit;
        if (!args.Result)
            _popup.PopupClient(Loc.GetString("heretic-ritual-fail-ghoul-limit"), user, user);
    }

    private void OnRustWall(Entity<TransformComponent> ent,
        ref HereticRitualConditionEvent<TryMakeRustWallCondition> args)
    {
        if (!TryGetValue(args.Ritual, Performer, out EntityUid user) ||
            !TryGetValue(args.Ritual, Mind, out EntityUid mind) || !TryComp(mind, out HereticComponent? heretic))
            return;

        args.Result = _ability.TryMakeRustWall(ent, user, heretic, args.Condition.RustStrengthOverride);
    }

    private void OnBackstab(Entity<TransformComponent> ent, ref HereticRitualConditionEvent<BackstabCondition> args)
    {
        if (!TryGetValue(args.Ritual, Performer, out EntityUid user))
            return;

        args.Result = _backStab.TryBackstab(ent,
            user,
            args.Condition.Tolerance,
            args.Condition.ShowPopup,
            args.Condition.PlaySound,
            args.Condition.AlwaysBackstabLaying);
    }

    private void OnMinLevel(Entity<TransformComponent> ent,
        ref HereticRitualConditionEvent<HereticMinPassiveLevelCondition> args)
    {
        if (!TryGetValue(args.Ritual, Mind, out EntityUid mind) || !TryComp(mind, out HereticComponent? heretic))
            return;

        args.Result = heretic.PassiveLevel >= args.Condition.MinLevel;
    }

    private void OnMinStage(Entity<TransformComponent> ent,
        ref HereticRitualConditionEvent<HereticMinStageCondition> args)
    {
        if (!TryGetValue(args.Ritual, Mind, out EntityUid mind) || !TryComp(mind, out HereticComponent? heretic))
            return;

        args.Result = heretic.PathStage >= args.Condition.MinStage;
    }

    private void OnLimitedCondition(Entity<TransformComponent> ent,
        ref HereticRitualConditionEvent<IsLimitedOutputCondition> args)
    {
        args.Result = Comp<HereticRitualComponent>(args.Ritual).LimitedOutput.Contains(ent.Owner);
    }

    private void OnApplyConditions(Entity<TransformComponent> ent,
        ref HereticRitualConditionEvent<ConditionsRitualCondition> args)
    {
        args.Result = args.Condition.RequireAll
            ? _effects.TryConditions(ent, args.Condition.Conditions, args.Ritual)
            : _effects.AnyCondition(ent, args.Condition.Conditions, args.Ritual);
    }

    private void OnApplySequence(Entity<HereticRitualComponent> ent,
        ref HereticRitualConditionEvent<TryApplyEffectSequenceCondition> args)
    {
        TryGetValue(args.Ritual, Performer, out EntityUid? user);

        args.Result = _effects.TryEffects(ent,
            ent.Comp.Effects.Skip(args.Condition.From).Take(args.Condition.To - args.Condition.From),
            args.Ritual,
            user);
    }

    private void OnObjectivesComplete(Entity<HereticComponent> ent,
        ref HereticRitualConditionEvent<ObjectivesCompleteCondition> args)
    {
        args.Result = _heretic.ObjectivesAllowAscension(ent);
    }

    private void OnCanAscend(Entity<HereticComponent> ent, ref HereticRitualConditionEvent<CanAscendCondition> args)
    {
        args.Result = ent.Comp.CanAscend;
    }

    private void OnProcessIngredients(Entity<HereticRitualComponent> ent,
        ref HereticRitualConditionEvent<ProcessIngredientsCondition> args)
    {
        if (args.Condition.ApplyOn == string.Empty)
            return;

        var missingList = new Dictionary<LocId, int>();
        var toDelete = new HashSet<EntityUid>();
        var toSplit = new Dictionary<Entity<StackComponent>, int>();

        var ingredients = CompOrNull<HereticKnowledgeRitualComponent>(ent)?.Ingredients ?? args.Condition.Ingredients;

        var ingredientAmounts = Enumerable.Repeat(0, ingredients.Count).ToList();

        foreach (var look in args.Ritual.Comp.Raiser.GetTargets<EntityUid>(args.Condition.ApplyOn))
        {
            for (var i = 0; i < ingredients.Count; i++)
            {
                var ritIng = ingredients[i];
                var compAmount = ingredientAmounts[i];

                if (compAmount >= ritIng.Amount)
                    continue;

                if (!_whitelist.CheckBoth(look, ritIng.Blacklist, ritIng.Whitelist))
                    continue;

                var stack = _stackQuery.CompOrNull(look);
                var amount = stack == null ? 1 : Math.Min(stack.Count, ritIng.Amount - compAmount);

                ingredientAmounts[i] += amount;

                if (stack == null || stack.Count <= amount)
                    toDelete.Add(look);
                else
                    toSplit.Add((look, stack), amount);
            }
        }

        for (var i = 0; i < ingredients.Count; i++)
        {
            var ritIng = ingredients[i];
            var difference = ritIng.Amount - ingredientAmounts[i];
            if (difference > 0)
                missingList.Add(ritIng.Name, difference);
        }

        if (missingList.Count == 0)
        {
            args.Result = true;
            args.Ritual.Comp.Blackboard[args.Condition.DeleteEntitiesKey] = toDelete;
            args.Ritual.Comp.Blackboard[args.Condition.SplitEntitiesKey] = toSplit;
            return;
        }

        var sb = new StringBuilder();
        foreach (var (name, amount) in missingList)
        {
            sb.Append($"{Loc.GetString(name)} x{amount} ");
        }

        sb.Remove(sb.Length - 1, 1);

        var str = Loc.GetString("heretic-ritual-fail-items", ("itemlist", sb.ToString()));
        CancelCondition(args.Ritual, ref args, str);
    }

    private void OnTargetCheck(Entity<TransformComponent> ent, ref HereticRitualConditionEvent<IsTargetCondition> args)
    {
        if (!TryGetValue(args.Ritual, Mind, out EntityUid mind) || !TryComp(mind, out HereticComponent? heretic))
        {
            CancelCondition(args.Ritual, ref args);
            return;
        }

        args.Result = IsSacrificeTarget((mind, heretic), ent);
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Heretic;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using System.Text;
using Content.Shared.Examine;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared._Shitcode.Heretic.Systems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityConditions;
using Content.Shared.Fluids.Components;
using Content.Shared.Gibbing;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Stacks;
using Content.Shared.Store.Components;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Player;

namespace Content.Shared._Shitcode.Heretic.Rituals;

public abstract class SharedHereticRitualSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFact = default!;

    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedEntityConditionsSystem _condition = default!;

    public SoundSpecifier RitualSuccessSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/castsummon.ogg");

    private EntityQuery<MobStateComponent> _mobQuery;
    private EntityQuery<GhoulComponent> _ghoulQuery;
    private EntityQuery<StackComponent> _stackQuery;
    private EntityQuery<FlammableComponent> _flammableQuery;
    private EntityQuery<PuddleComponent> _puddleQuery;
    private EntityQuery<TagComponent> _tagQuery;

    public const string Performer = "Performer";
    public const string Mind = "Mind";
    public const string Platform = "Platform";
    public const string CancelString = "CancelString";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticRitualRuneComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<HereticRitualRuneComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<HereticRitualRuneComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HereticRitualRuneComponent, HereticRitualMessage>(OnRitualChosenMessage);

        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<LookupCondition>>(OnLookup);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<FilterHereticsCondition>>(OnFilterHeretics);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<FilterCondition>>(OnFilter);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<FilterMobStateCondition>>(OnMobStateFilter);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<FilterTargetsCondition>>(OnTargetsFilter);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<CombineCondition>>(OnCombine);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<TakeNumberCondition>>(OnTakeNumber);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<SacrificeCondition>>(OnSacrifice);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<SpawnCondition>>(OnSpawn);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<PathBasedSpawnCondition>>(OnPathSpawn);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<ProcessIngredientsCondition>>(
            OnProcessIngredients);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<RaiseHereticEventCondition>>(OnRaiseEvent);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<AddKnowledgeCondition>>(OnAddKnowledge);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<FindLostLimitedOutputCondition>>(
            OnFindLimited);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<CanAscendCondition>>(OnCanAscend);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<ObjectivesCompleteCondition>>(
            OnObjectivesComlete);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<FilterOnFireCondition>>(OnFireFilter);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<FilterHeadlessCondition>>(OnHeadlessFilter);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<FilterReagentPuddleCondition>>(
            OnReagentFilter);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<DeleteEntityHashsetCondition>>(OnDelete);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<GhoulifyCondition>>(OnGhoulify);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<AddComponentsCondition>>(OnAddComponents);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<FilterKnowledgeTagsCondition>>(OnKnowledge);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<UpdateKnowledgeCondition>>(OnUpdateKnowledge);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<RemoveRitualsCondition>>(OnRemoveRituals);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<TeleportToRuneCondition>>(OnTeleport);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<ApplyConditionsCondition>>(OnApply);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<LowTemperatureCondition>>(OnLowTemperature);
        SubscribeLocalEvent<HereticRitualComponent, EntityConditionEvent<OpenRuneBuiCondition>>(OnBui);

        _mobQuery = GetEntityQuery<MobStateComponent>();
        _ghoulQuery = GetEntityQuery<GhoulComponent>();
        _stackQuery = GetEntityQuery<StackComponent>();
        _flammableQuery = GetEntityQuery<FlammableComponent>();
        _puddleQuery = GetEntityQuery<PuddleComponent>();
        _tagQuery = GetEntityQuery<TagComponent>();
    }

    #region Conditions
    private void OnBui(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<OpenRuneBuiCondition> args)
    {
        if (!TryGetValue(ent, Platform, out EntityUid platform) ||
            !TryGetValue(ent, Performer, out EntityUid performer))
        {
            CancelCondition(ent, ref args);
            return;
        }

        _uiSystem.OpenUi(platform, args.Condition.Key, performer);
        args.Result = true;
    }

    private void OnLowTemperature(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<LowTemperatureCondition> args)
    {
        if (!TryGetValue(ent, Platform, out EntityUid platform) ||
            !TryComp(platform, out TemperatureTrackerComponent? tracker) ||
            tracker.Temperature > Atmospherics.T0C + args.Condition.Threshold)
        {
            CancelCondition(ent, ref args);
            return;
        }

        args.Result = true;
    }

    private void OnApply(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<ApplyConditionsCondition> args)
    {
        args.Result = ApplyConditions(ent,
            ent.Comp.Conditions.Skip(args.Condition.FromIndex)
                .Where(x => x is not ApplyConditionsCondition)
                .Take(args.Condition.ToIndex - args.Condition.FromIndex));
    }

    private void OnTeleport(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<TeleportToRuneCondition> args)
    {
        if (!TryGetValue(ent, args.Condition.InputKey, out EntityUid? input) ||
            !TryGetValue(ent, Platform, out EntityUid platform))
        {
            CancelCondition(ent, ref args);
            return;
        }

        var coords = _transform.GetMapCoordinates(platform);
        _transform.SetMapCoordinates(input.Value, coords);
        args.Result = true;
    }

    private void OnRemoveRituals(Entity<HereticRitualComponent> ent,
        ref EntityConditionEvent<RemoveRitualsCondition> args)
    {
        if (!TryGetValue(ent, Mind, out EntityUid mind))
        {
            CancelCondition(ent, ref args);
            return;
        }

        _heretic.RemoveRituals(mind, args.Condition.RitualTags);
        args.Result = true;
    }

    private void OnUpdateKnowledge(Entity<HereticRitualComponent> ent,
        ref EntityConditionEvent<UpdateKnowledgeCondition> args)
    {
        if (!TryGetValue(ent, Performer, out EntityUid performer))
        {
            CancelCondition(ent, ref args);
            return;
        }

        _heretic.UpdateKnowledge(performer, args.Condition.Amount);
        args.Result = true;
    }

    private void OnKnowledge(Entity<HereticRitualComponent> ent,
        ref EntityConditionEvent<FilterKnowledgeTagsCondition> args)
    {
        if (!TryGetValue(ent, args.Condition.InputKey, out HashSet<EntityUid>? input) ||
            !TryComp(ent, out HereticKnowledgeRitualComponent? knowledge))
        {
            CancelCondition(ent, ref args);
            return;
        }

        var output = new HashSet<EntityUid>();
        var missingTags = knowledge.KnowledgeRequiredTags.ToHashSet();
        foreach (var uid in input)
        {
            if (!_tagQuery.TryComp(uid, out var tags))
                continue;

            missingTags.RemoveWhere(tag =>
            {
                if (!_tag.HasTag(tags, tag))
                    return false;

                output.Add(uid);
                return true;
            });
        }

        if (missingTags.Count > 0)
        {
            var missing = string.Join(", ", missingTags);
            var cancelString = Loc.GetString("heretic-ritual-fail-items", ("itemlist", missing));
            CancelCondition(ent, ref args, cancelString);
            return;
        }

        ent.Comp.Blackboard[args.Condition.OutputKey] = output;
        args.Result = true;
    }

    private void OnAddComponents(Entity<HereticRitualComponent> ent,
        ref EntityConditionEvent<AddComponentsCondition> args)
    {
        if (!TryGetValue(ent, args.Condition.InputKey, out HashSet<EntityUid>? input))
        {
            CancelCondition(ent, ref args);
            return;
        }

        foreach (var uid in input)
        {
            EntityManager.AddComponents(uid, args.Condition.Components);
        }

        args.Result = true;
    }

    private void OnGhoulify(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<GhoulifyCondition> args)
    {
        if (!TryGetValue(ent, Performer, out EntityUid performer) ||
            !TryGetValue(ent, args.Condition.InputKey, out HashSet<EntityUid>? input))
        {
            CancelCondition(ent, ref args);
            return;
        }

        HashSet<EntityUid> output = new();

        foreach (var uid in input)
        {
            output.Add(uid);

            var minion = _compFact.GetComponent<HereticMinionComponent>();
            minion.BoundHeretic = performer;
            AddComp(uid, minion, true);

            var ghoul = _compFact.GetComponent<GhoulComponent>();
            ghoul.TotalHealth = args.Condition.TotalHealth;
            ghoul.GiveBlade = args.Condition.GiveBlade;
            AddComp(uid, ghoul, true);

            if (ent.Comp.Limit <= 0)
                continue;

            ent.Comp.LimitedOutput.Add(uid);
            if (ent.Comp.LimitedOutput.Count >= ent.Comp.Limit)
                break;
        }

        OutputHashset(ent, output, ref args);
    }

    private void OnDelete(Entity<HereticRitualComponent> ent,
        ref EntityConditionEvent<DeleteEntityHashsetCondition> args)
    {
        if (!TryGetValue(ent, args.Condition.InputKey, out HashSet<EntityUid>? input))
        {
            CancelCondition(ent, ref args);
            return;
        }

        foreach (var uid in input)
        {
            QueueDel(uid);
        }

        args.Result = true;
    }

    private void OnReagentFilter(Entity<HereticRitualComponent> ent,
        ref EntityConditionEvent<FilterReagentPuddleCondition> args)
    {
        var cancelStr = Loc.GetString("heretic-ritual-fail-reagentpuddle",
            ("reagentname", Loc.GetString(args.Condition.ReagentLoc)));

        if (!TryGetValue(ent, args.Condition.InputKey, out HashSet<EntityUid>? input))
        {
            CancelCondition(ent, ref args, cancelStr);
            return;
        }

        var reagents = args.Condition.Reagents;

        HashSet<EntityUid> output = new();

        foreach (var uid in input)
        {
            if (!_puddleQuery.TryComp(uid, out var puddle))
                continue;

            if (puddle.Solution == null)
                continue;

            var soln = puddle.Solution.Value;

            if (!soln.Comp.Solution.Any(x => reagents.Contains(x.Reagent.Prototype)))
                continue;

            output.Add(uid);
        }

        OutputHashset(ent, output, ref args, cancelStr);
    }

    private void OnHeadlessFilter(Entity<HereticRitualComponent> ent,
        ref EntityConditionEvent<FilterHeadlessCondition> args)
    {
        if (!TryGetValue(ent, args.Condition.InputKey, out HashSet<EntityUid>? input))
        {
            CancelCondition(ent, ref args);
            return;
        }

        HashSet<EntityUid> output = new();

        foreach (var uid in input)
        {
            if (!_body.GetBodyChildrenOfType(uid, BodyPartType.Head).Any())
                output.Add(uid);
        }

        OutputHashset(ent, output, ref args);
    }

    private void OnFireFilter(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<FilterOnFireCondition> args)
    {
        if (!TryGetValue(ent, args.Condition.InputKey, out HashSet<EntityUid>? input))
        {
            CancelCondition(ent, ref args);
            return;
        }

        HashSet<EntityUid> output = new();

        foreach (var uid in input)
        {
            if (_flammableQuery.TryComp(uid, out var flam) && flam.OnFire)
                output.Add(uid);
        }

        OutputHashset(ent, output, ref args);
    }

    private void OnObjectivesComlete(Entity<HereticRitualComponent> ent,
        ref EntityConditionEvent<ObjectivesCompleteCondition> args)
    {
        if (!TryGetValue(ent, Mind, out EntityUid mind) || !_heretic.ObjectivesAllowAscension(mind))
        {
            CancelCondition(ent, ref args);
            return;
        }

        args.Result = true;
    }

    private void OnCanAscend(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<CanAscendCondition> args)
    {
        if (!TryGetValue(ent, Mind, out EntityUid mind) || !TryComp(mind, out HereticComponent? heretic) ||
            !heretic.CanAscend)
        {
            CancelCondition(ent, ref args);
            return;
        }

        args.Result = true;
    }

    private void OnFindLimited(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<FindLostLimitedOutputCondition> args)
    {
        if (ent.Comp.LimitedOutput.Count == 0 || !TryGetValue(ent, Platform, out EntityUid platform))
        {
            CancelCondition(ent, ref args);
            return;
        }

        var coords = _transform.GetMapCoordinates(platform);
        EntityUid? selected = null;
        var maxDist = args.Condition.MinRange;

        foreach (var output in ent.Comp.LimitedOutput)
        {
            var outCoords = _transform.GetMapCoordinates(output);
            if (outCoords.MapId != coords.MapId)
            {
                selected = output;
                break;
            }

            var dist = (coords.Position - outCoords.Position).Length();

            if (dist < args.Condition.MinRange)
                continue;

            if (dist < maxDist)
                continue;

            maxDist = dist;
            selected = output;
        }

        if (selected is not { } uid)
        {
            if (args.Condition.CancelOnEmptyOutput)
                CancelCondition(ent, ref args);
            return;
        }

        ent.Comp.Blackboard[args.Condition.OutputKey] = uid;
        args.Result = true;
    }

    private void OnAddKnowledge(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<AddKnowledgeCondition> args)
    {
        if (!TryGetValue(ent, Mind, out EntityUid mind) ||
            !_heretic.TryAddKnowledge(mind, args.Condition.Knowledge))
        {
            CancelCondition(ent, ref args);
            return;
        }

        args.Result = true;
    }

    private void OnRaiseEvent(Entity<HereticRitualComponent> ent,
        ref EntityConditionEvent<RaiseHereticEventCondition> args)
    {
        if (args.Condition.Event is not { } ev || !TryGetValue(ent, Mind, out EntityUid mind))
        {
            CancelCondition(ent, ref args);
            return;
        }

        RaiseLocalEvent(mind, ev, true);
        args.Result = true;
    }

    private void OnProcessIngredients(Entity<HereticRitualComponent> ent,
        ref EntityConditionEvent<ProcessIngredientsCondition> args)
    {
        if (!TryGetValue(ent, args.Condition.InputKey, out HashSet<EntityUid>? input))
        {
            CancelCondition(ent, ref args);
            return;
        }

        var missingList = new Dictionary<LocId, int>();
        var toDelete = new List<EntityUid>();
        var toSplit = new List<(Entity<StackComponent> uid, int amount)>();

        var ingredientAmounts = Enumerable.Repeat(0, args.Condition.Ingredients.Count).ToList();

        foreach (var look in input)
        {
            for (var i = 0; i < args.Condition.Ingredients.Count; i++)
            {
                var ritIng = args.Condition.Ingredients[i];
                var compAmount = ingredientAmounts[i];

                if (compAmount >= ritIng.Amount)
                    continue;

                if (_whitelist.IsWhitelistFail(ritIng.Whitelist, look))
                    continue;

                var stack = _stackQuery.CompOrNull(look);
                var amount = stack == null ? 1 : Math.Min(stack.Count, ritIng.Amount - compAmount);

                ingredientAmounts[i] += amount;

                if (stack == null || stack.Count <= amount)
                    toDelete.Add(look);
                else
                    toSplit.Add(((look, stack), amount));
            }
        }

        for (var i = 0; i < args.Condition.Ingredients.Count; i++)
        {
            var ritIng = args.Condition.Ingredients[i];
            var difference = ritIng.Amount - ingredientAmounts[i];
            if (difference > 0)
                missingList.Add(ritIng.Name, difference);
        }

        if (missingList.Count == 0)
        {
            foreach (var uid in toDelete)
            {
                QueueDel(uid);
            }

            foreach (var (stackEnt, amount) in toSplit)
            {
                _stack.SetCount(stackEnt.AsNullable(), stackEnt.Comp.Count - amount);
            }

            args.Result = true;
            return;
        }

        var sb = new StringBuilder();
        foreach (var (name, amount) in missingList)
        {
            sb.Append($"{Loc.GetString(name)} x{amount} ");
        }

        sb.Remove(sb.Length - 1, 1);

        var str = Loc.GetString("heretic-ritual-fail-items", ("itemlist", sb.ToString()));
        CancelCondition(ent, ref args, str);
    }

    private void OnPathSpawn(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<PathBasedSpawnCondition> args)
    {
        if (!TryGetValue(ent, Platform, out EntityUid platform) || !TryGetValue(ent, Mind, out EntityUid mind) ||
            !TryComp(mind, out HereticComponent? heretic))
        {
            CancelCondition(ent, ref args);
            return;
        }

        var coords = Transform(platform).Coordinates;

        EntityUid spawned;
        if (heretic.CurrentPath is { } path && args.Condition.Output.TryGetValue(path, out var toSpawn))
            spawned = Spawn(toSpawn, coords);
        else
            spawned = Spawn(args.Condition.FallbackOutput, coords);

        args.Result = true;

        if (ent.Comp.Limit <= 0)
            return;

        ent.Comp.LimitedOutput.Add(spawned);
    }

    private void OnSpawn(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<SpawnCondition> args)
    {
        if (!TryGetValue(ent, Platform, out EntityUid platform) ||
            !TryGetValue(ent, Performer, out EntityUid performer))
        {
            CancelCondition(ent, ref args);
            return;
        }

        var coords = _transform.GetMapCoordinates(platform);
        foreach (var (obj, amount) in args.Condition.Output)
        {
            for (var i = 0; i < amount; i++)
            {
                var spawned = Spawn(obj, coords);

                if (_ghoulQuery.HasComp(spawned))
                {
                    var ev = new SetGhoulBoundHereticEvent(performer);
                    RaiseLocalEvent(spawned, ref ev);
                }

                if (ent.Comp.Limit <= 0)
                    continue;

                ent.Comp.LimitedOutput.Add(spawned);
                if (ent.Comp.LimitedOutput.Count >= ent.Comp.Limit)
                    break;
            }
        }
    }


    private void OnSacrifice(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<SacrificeCondition> args)
    {
        if (!TryGetValue(ent, args.Condition.InputKey, out HashSet<EntityUid>? input) ||
            !TryGetValue(ent, Mind, out EntityUid mind) ||
            !TryComp(mind, out MindComponent? mindComp) || !TryComp(mind, out StoreComponent? store) ||
            !TryComp(mind, out HereticComponent? heretic))
        {
            CancelCondition(ent, ref args);
            return;
        }

        var knowledgeGain = 0f;
        foreach (var uid in input)
        {
            var (isCommand, isSec) = IsCommandOrSec(uid);
            var isHeretic = _heretic.TryGetHereticComponent(uid, out _, out _);
            knowledgeGain += isHeretic || IsSacrificeTarget((mind, heretic), uid)
                ? isCommand || isSec || isHeretic ? 3f : 2f
                : 0f;

            _gibbing.Gib(uid);

            var ev = new IncrementHereticObjectiveProgressEvent(args.Condition.SacrificeObjective);
            RaiseLocalEvent(mind, ref ev);

            if (!isCommand)
                continue;

            var ev2 = new IncrementHereticObjectiveProgressEvent(args.Condition.SacrificeHeadObjective);
            RaiseLocalEvent(mind, ref ev2);
        }

        if (knowledgeGain > 0)
            _heretic.UpdateMindKnowledge((mind, heretic, store, mindComp), null, knowledgeGain);

        args.Result = true;
    }

    private void OnTakeNumber(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<TakeNumberCondition> args)
    {
        if (!TryGetValue(ent, args.Condition.InputKey, out HashSet<EntityUid>? input) || input.Count < args.Condition.Number)
        {
            CancelCondition(ent, ref args);
            return;
        }

        var output = input.Take(args.Condition.Number).ToHashSet();

        OutputHashset(ent, output, ref args);
    }

    private void OnCombine(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<CombineCondition> args)
    {
        if (!TryGetValue(ent, args.Condition.InputKey, out HashSet<EntityUid>? input1) ||
            !TryGetValue(ent, args.Condition.InputKey2, out HashSet<EntityUid>? input2))
        {
            CancelCondition(ent, ref args);
            return;
        }

        var output = input1.Concat(input2).ToHashSet();

        OutputHashset(ent, output, ref args);
    }

    private void OnTargetsFilter(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<FilterTargetsCondition> args)
    {
        if (!TryGetValue(ent, args.Condition.InputKey, out HashSet<EntityUid>? input) ||
            !TryGetValue(ent, Mind, out EntityUid mind) || !TryComp(mind, out HereticComponent? heretic))
        {
            CancelCondition(ent, ref args);
            return;
        }

        HashSet<EntityUid> output = new();
        foreach (var uid in input)
        {
            if (IsSacrificeTarget((mind, heretic), uid))
                output.Add(uid);
        }

        OutputHashset(ent, output, ref args);
    }

    private void OnMobStateFilter(Entity<HereticRitualComponent> ent,
        ref EntityConditionEvent<FilterMobStateCondition> args)
    {
        if (!TryGetValue(ent, args.Condition.InputKey, out HashSet<EntityUid>? input))
        {
            CancelCondition(ent, ref args);
            return;
        }

        HashSet<EntityUid> output = new();
        foreach (var uid in input)
        {
            if (!_mobQuery.TryComp(uid, out var mob))
                continue;

            if ((mob.CurrentState == args.Condition.MobState) ^ args.Condition.InvertCheck)
                output.Add(uid);
        }

        OutputHashset(ent, output, ref args);
    }

    private void OnFilter(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<FilterCondition> args)
    {
        if (!TryGetValue(ent, args.Condition.InputKey, out HashSet<EntityUid>? input))
        {
            CancelCondition(ent, ref args);
            return;
        }

        HashSet<EntityUid> output = new();
        foreach (var uid in input)
        {
            if (_whitelist.CheckBoth(uid, args.Condition.Blacklist, args.Condition.Whitelist))
                output.Add(uid);
        }

        OutputHashset(ent, output, ref args);
    }

    private void OnFilterHeretics(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<FilterHereticsCondition> args)
    {
        if (!TryGetValue(ent, args.Condition.InputKey, out HashSet<EntityUid>? input))
        {
            CancelCondition(ent, ref args);
            return;
        }

        HashSet<EntityUid> output = new();
        foreach (var uid in input)
        {
            if (_heretic.TryGetHereticComponent(uid, out _, out _))
                output.Add(uid);
        }

        OutputHashset(ent, output, ref args);
    }

    private void OnLookup(Entity<HereticRitualComponent> ent, ref EntityConditionEvent<LookupCondition> args)
    {
        if (!TryGetValue(ent, Platform, out EntityUid platform))
        {
            CancelCondition(ent, ref args);
            return;
        }

        var look = _lookup.GetEntitiesInRange(platform, args.Condition.Range, args.Condition.Flags);
        OutputHashset(ent, look, ref args);
    }

    #endregion

    #region Helpers

    protected virtual (bool isCommand, bool isSec) IsCommandOrSec(EntityUid uid)
    {
        return (false, false);
    }

    private bool IsSacrificeTarget(Entity<HereticComponent> heretic, EntityUid target)
    {
        return heretic.Comp.SacrificeTargets.Any(x => x.Entity == GetNetEntity(target));
    }

    private void OutputHashset<T>(Entity<HereticRitualComponent> ent,
        HashSet<EntityUid> output,
        ref EntityConditionEvent<T> args,
        string? cancelStr = null) where T : OutputCondition<T>
    {
        if (args.Condition.CancelOnEmptyOutput && output.Count == 0)
        {
            CancelCondition(ent, ref args, cancelStr);
            return;
        }

        ent.Comp.Blackboard[args.Condition.OutputKey] = output;
        args.Result = true;
    }

    private void CancelCondition<T>(Entity<HereticRitualComponent> ent,
        ref EntityConditionEvent<T> ev,
        string? cancelString = null)
        where T : BaseHereticRitualCondition<T>
    {
        ev.Result = false;

        if (cancelString != null)
            ent.Comp.Blackboard[CancelString] = cancelString;
        else if (ev.Condition.CancelLoc is { } loc)
            ent.Comp.Blackboard[CancelString] = Loc.GetString(loc);
    }

    protected bool TryGetValue<T>(Entity<HereticRitualComponent> ent, string key, [NotNullWhen(true)] out T? value)
    {
        if (ent.Comp.Blackboard.TryGetValue(key, out var val))
        {
            value = (T) val;
            return true;
        }

        value = default;
        return false;
    }

    public bool ApplyConditions(Entity<HereticRitualComponent> ent, IEnumerable<EntityCondition> conditions)
    {
        foreach (var cond in conditions)
        {
            if (!_condition.TryCondition(ent, cond))
                return false;
        }

        return true;
    }

    private bool TryDoRitual(Entity<HereticRitualComponent> ent,
        EntityUid performer,
        EntityUid platform)
    {
        if (ent.Comp.Limit > 0)
        {
            ent.Comp.LimitedOutput = ent.Comp.LimitedOutput.Where(Exists).ToList();
            if (ent.Comp.LimitedOutput.Count >= ent.Comp.Limit)
            {
                if (ent.Comp.LimitReachedConditions.Count > 0)
                {
                    return ApplyConditions(ent, ent.Comp.LimitReachedConditions);
                }

                _popup.PopupClient(Loc.GetString("heretic-ritual-fail-limit"), platform, performer);
                return false;
            }
        }

        return ApplyConditions(ent, ent.Comp.Conditions);
    }

    private void SetupBlackboard(Entity<HereticRitualComponent> ent,
        EntityUid performer,
        EntityUid mind,
        EntityUid platform)
    {
        ent.Comp.Blackboard.Clear();
        ent.Comp.Blackboard[Performer] = performer;
        ent.Comp.Blackboard[Mind] = mind;
        ent.Comp.Blackboard[Platform] = platform;
        if (ent.Comp.CancelLoc is { } loc)
            ent.Comp.Blackboard[CancelString] = Loc.GetString(loc);
    }

    #endregion

    #region RitualRuneEvents

    private void OnInteract(Entity<HereticRitualRuneComponent> ent, ref InteractHandEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.User, out var heretic, out _))
            return;

        if (heretic.Rituals.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("heretic-ritual-norituals"), args.User, args.User);
            return;
        }

        _uiSystem.OpenUi(ent.Owner, HereticRitualRuneUiKey.Key, args.User);
    }

    private void OnRitualChosenMessage(Entity<HereticRitualRuneComponent> ent, ref HereticRitualMessage args)
    {
        var user = args.Actor;

        if (!_heretic.TryGetHereticComponent(user, out var heretic, out _))
            return;

        heretic.ChosenRitual = GetEntity(args.Ritual);

        var ritualName = Name(heretic.ChosenRitual.Value);
        _popup.PopupClient(Loc.GetString("heretic-ritual-switch", ("name", ritualName)), user, user);
    }

    private void OnInteractUsing(Entity<HereticRitualRuneComponent> ent, ref InteractUsingEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.User, out var heretic, out var mind))
            return;

        if (!HasComp<MansusGraspComponent>(args.Used))
            return;

        if (!TryComp(heretic.ChosenRitual, out HereticRitualComponent? ritual))
        {
            _popup.PopupClient(Loc.GetString("heretic-ritual-noritual"), args.User, args.User);
            return;
        }

        Entity<HereticRitualComponent> ritEnt = (heretic.ChosenRitual.Value, ritual);

        SetupBlackboard(ritEnt, args.User, mind, ent);

        if (TryDoRitual(ritEnt, args.User, ent))
        {
            if (ritual.PlaySuccessAnimation)
                RitualSuccess(ent, args.User, true);
        }
        else if (TryGetValue(ritEnt, CancelString, out string? cancelStr))
            _popup.PopupClient(cancelStr, ent, args.User);

        ritual.Blackboard.Clear();
    }

    private void OnExamine(Entity<HereticRitualRuneComponent> ent, ref ExaminedEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.Examiner, out var h, out _))
            return;

        var name = h.ChosenRitual != null ? Name(h.ChosenRitual.Value) : Loc.GetString("heretic-ritual-none");
        args.PushMarkup(Loc.GetString("heretic-ritualrune-examine", ("rit", name)));
    }

    public void RitualSuccess(EntityUid ent, EntityUid user, bool predicted)
    {
        _audio.PlayPredicted(RitualSuccessSound, ent, predicted ? user : null, AudioParams.Default.WithVolume(-3f));
        var popup = Loc.GetString("heretic-ritual-success");
        _popup.PopupPredicted(popup, ent, predicted ? user : null, Filter.Entities(user), false);
        PredictedSpawnAtPosition("HereticRuneRitualAnimation", Transform(ent).Coordinates);
    }

    #endregion
}

// SPDX-FileCopyrightText: 2024 BombasterDS <115770678+BombasterDS@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 whateverusername0 <whateveremail>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Heretic.Components;
using Content.Shared.Heretic;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using System.Text;
using Content.Server._Goobstation.Objectives.Components;
using Content.Server._Goobstation.Objectives.Systems;
using Content.Server._Shitcode.Heretic.Ui;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.EUI;
using Content.Server.Mind;
using Content.Server.Revolutionary.Components;
using Content.Shared.Examine;
using Content.Shared._Goobstation.Heretic.Components;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Part;
using Content.Shared.Fluids.Components;
using Content.Shared.Gibbing;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Stacks;
using Content.Shared.Store.Components;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.EntitySystems;

public sealed class HereticRitualSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly GhoulSystem _ghoul = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public SoundSpecifier RitualSuccessSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/castsummon.ogg");

    private EntityQuery<MobStateComponent> _mobQuery;
    private EntityQuery<GhoulComponent> _ghoulQuery;
    private EntityQuery<CommandStaffComponent> _commandQuery;
    private EntityQuery<SecurityStaffComponent> _securityQuery;
    private EntityQuery<StackComponent> _stackQuery;
    private EntityQuery<FlammableComponent> _flammableQuery;
    private EntityQuery<PuddleComponent> _puddleQuery;
    private EntityQuery<TagComponent> _tagQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticRitualRuneComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<HereticRitualRuneComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<HereticRitualRuneComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HereticRitualRuneComponent, HereticRitualMessage>(OnRitualChosenMessage);

        SubscribeLocalEvent<HereticRitualComponent, LookupRitualEvent>(OnLookup);
        SubscribeLocalEvent<HereticRitualComponent, FilterHereticsRitualEvent>(OnFilterHeretics);
        SubscribeLocalEvent<HereticRitualComponent, FilterRitualEvent>(OnFilter);
        SubscribeLocalEvent<HereticRitualComponent, FilterByMobStateRitualEvent>(OnMobStateFilter);
        SubscribeLocalEvent<HereticRitualComponent, FilterTargetsRitualEvent>(OnTargetsFilter);
        SubscribeLocalEvent<HereticRitualComponent, CombineEntityHashSetRitualEvent>(OnCombine);
        SubscribeLocalEvent<HereticRitualComponent, TakeNumberEntitiesRitualEvent>(OnTakeNumber);
        SubscribeLocalEvent<HereticRitualComponent, SacrificeRitualEvent>(OnSacrifice);
        SubscribeLocalEvent<HereticRitualComponent, SpawnRitualEvent>(OnSpawn);
        SubscribeLocalEvent<HereticRitualComponent, PathBasedSpawnRitualEvent>(OnPathSpawn);
        SubscribeLocalEvent<HereticRitualComponent, ProcessIngredientsRitualEvent>(OnProcessIngredients);
        SubscribeLocalEvent<HereticRitualComponent, RaiseHereticEventRitualEvent>(OnRaiseEvent);
        SubscribeLocalEvent<HereticRitualComponent, AddKnowledgeRitualEvent>(OnAddKnowledge);
        SubscribeLocalEvent<HereticRitualComponent, FindLostLimitedOutputRitualEvent>(OnFindLimited);
        SubscribeLocalEvent<HereticRitualComponent, CanAscendRitualEvent>(OnCanAscend);
        SubscribeLocalEvent<HereticRitualComponent, ObjectivesCompleteRitualEvent>(OnObjectivesComlete);
        SubscribeLocalEvent<HereticRitualComponent, FilterOnFireRitualEvent>(OnFireFilter);
        SubscribeLocalEvent<HereticRitualComponent, FilterHeadlessRitualEvent>(OnHeadlessFilter);
        SubscribeLocalEvent<HereticRitualComponent, FilterReagentPuddleRitualEvent>(OnReagentFilter);
        SubscribeLocalEvent<HereticRitualComponent, DeleteEntityHashsetRitualEvent>(OnDelete);
        SubscribeLocalEvent<HereticRitualComponent, GhoulifyRitualEvent>(OnGhoulify);
        SubscribeLocalEvent<HereticRitualComponent, AddComponentsRitualEvent>(OnAddComponents);
        SubscribeLocalEvent<HereticRitualComponent, LowTemperatureRitualEvent>(OnLowTemperature);
        SubscribeLocalEvent<HereticRitualComponent, FilterKnowledgeTagsRitualEvent>(OnKnowledge);
        SubscribeLocalEvent<HereticRitualComponent, UpdateKnowledgeRitualEvent>(OnUpdateKnowledge);
        SubscribeLocalEvent<HereticRitualComponent, RemoveRitualsRitualEvent>(OnRemoveRituals);
        SubscribeLocalEvent<HereticRitualComponent, FeastOfOwlsMenuRitualEvent>(OnFeastOfOwls);
        SubscribeLocalEvent<HereticRitualComponent, TeleportToRuneRitualEvent>(OnTeleport);
        SubscribeLocalEvent<HereticRitualComponent, RaiseRitualEventsRitualEvent>(OnRaise);

        _mobQuery = GetEntityQuery<MobStateComponent>();
        _ghoulQuery = GetEntityQuery<GhoulComponent>();
        _commandQuery = GetEntityQuery<CommandStaffComponent>();
        _securityQuery = GetEntityQuery<SecurityStaffComponent>();
        _stackQuery = GetEntityQuery<StackComponent>();
        _flammableQuery = GetEntityQuery<FlammableComponent>();
        _puddleQuery = GetEntityQuery<PuddleComponent>();
        _tagQuery = GetEntityQuery<TagComponent>();
    }

    #region RitualEvents

    private void OnRaise(Entity<HereticRitualComponent> ent, ref RaiseRitualEventsRitualEvent args)
    {
        if (!RaiseRitualEvents(ent,
                ent.Comp.Events.Skip(args.FromIndex)
                    .Where(x => x is not RaiseRitualEventsRitualEvent)
                    .Take(args.ToIndex - args.FromIndex),
                args.Performer,
                args.Mind,
                args.Platform))
            args.Cancel();
    }

    private void OnTeleport(Entity<HereticRitualComponent> ent, ref TeleportToRuneRitualEvent args)
    {
        if (!TryGetValue(ent, args.InputKey, out EntityUid? input))
        {
            args.Cancel();
            return;
        }

        var coords = _transform.GetMapCoordinates(args.Platform);
        _transform.SetMapCoordinates(input.Value, coords);
    }

    private void OnFeastOfOwls(Entity<HereticRitualComponent> ent, ref FeastOfOwlsMenuRitualEvent args)
    {
        if (!TryComp(args.Mind.Owner, out MindComponent? mind) || mind.UserId is not { } id ||
            !_player.TryGetSessionById(id, out var session))
        {
            args.Cancel();
            return;
        }

        _eui.OpenEui(new FeastOfOwlsEui(args.Performer, args.Mind, args.Platform, EntityManager), session);
    }

    private void OnRemoveRituals(Entity<HereticRitualComponent> ent, ref RemoveRitualsRitualEvent args)
    {
        _heretic.RemoveRituals(args.Mind, args.RitualTags);
    }

    private void OnUpdateKnowledge(Entity<HereticRitualComponent> ent, ref UpdateKnowledgeRitualEvent args)
    {
        if (!TryComp(args.Mind, out MindComponent? mindComp) || !TryComp(args.Mind, out StoreComponent? store))
        {
            args.Cancel();
            return;
        }

        _heretic.UpdateMindKnowledge((args.Mind, args.Mind, store, mindComp), args.Performer, args.Amount);
    }

    private void OnKnowledge(Entity<HereticRitualComponent> ent, ref FilterKnowledgeTagsRitualEvent args)
    {
        if (!TryGetValue(ent, args.InputKey, out HashSet<EntityUid>? input) ||
            !TryComp(ent, out HereticKnowledgeRitualComponent? knowledge))
        {
            args.Cancel();
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
            args.CancelStringOverride = Loc.GetString("heretic-ritual-fail-items", ("itemlist", missing));
            args.Cancel();
            return;
        }

        ent.Comp.Blackboard[args.OutputKey] = output;
    }

    private void OnLowTemperature(Entity<HereticRitualComponent> ent, ref LowTemperatureRitualEvent args)
    {
        var mix = _atmos.GetTileMixture(args.Platform);

        if (mix == null || mix.TotalMoles == 0)
            return;

        if (mix.Temperature > Atmospherics.T0C + args.Threshold)
            args.Cancel();
    }

    private void OnAddComponents(Entity<HereticRitualComponent> ent, ref AddComponentsRitualEvent args)
    {
        if (!TryGetValue(ent, args.InputKey, out HashSet<EntityUid>? input))
        {
            args.Cancel();
            return;
        }

        foreach (var uid in input)
        {
            EntityManager.AddComponents(uid, args.Components);
        }
    }

    private void OnGhoulify(Entity<HereticRitualComponent> ent, ref GhoulifyRitualEvent args)
    {
        if (!TryGetValue(ent, args.InputKey, out HashSet<EntityUid>? input))
        {
            args.Cancel();
            return;
        }

        HashSet<EntityUid> output = new();

        foreach (var uid in input)
        {
            output.Add(uid);

            var minion = _compFact.GetComponent<HereticMinionComponent>();
            minion.BoundHeretic = args.Performer;
            AddComp(uid, minion, true);

            var ghoul = _compFact.GetComponent<GhoulComponent>();
            ghoul.TotalHealth = args.TotalHealth;
            ghoul.GiveBlade = args.GiveBlade;
            AddComp(uid, ghoul, true);

            if (ent.Comp.Limit <= 0)
                continue;

            ent.Comp.LimitedOutput.Add(uid);
            if (ent.Comp.LimitedOutput.Count >= ent.Comp.Limit)
                break;
        }

        OutputHashset(ent, output, args);
    }

    private void OnDelete(Entity<HereticRitualComponent> ent, ref DeleteEntityHashsetRitualEvent args)
    {
        if (!TryGetValue(ent, args.InputKey, out HashSet<EntityUid>? input))
        {
            args.Cancel();
            return;
        }

        foreach (var uid in input)
        {
            QueueDel(uid);
        }
    }

    private void OnReagentFilter(Entity<HereticRitualComponent> ent, ref FilterReagentPuddleRitualEvent args)
    {
        args.CancelStringOverride = Loc.GetString("heretic-ritual-fail-reagentpuddle",
            ("reagentname", Loc.GetString(args.ReagentLoc)));

        if (!TryGetValue(ent, args.InputKey, out HashSet<EntityUid>? input))
        {
            args.Cancel();
            return;
        }

        var reagents = args.Reagents;

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

        OutputHashset(ent, output, args);
    }

    private void OnHeadlessFilter(Entity<HereticRitualComponent> ent, ref FilterHeadlessRitualEvent args)
    {
        if (!TryGetValue(ent, args.InputKey, out HashSet<EntityUid>? input))
        {
            args.Cancel();
            return;
        }

        HashSet<EntityUid> output = new();

        foreach (var uid in input)
        {
            if (!_body.GetBodyChildrenOfType(uid, BodyPartType.Head).Any())
                output.Add(uid);
        }

        OutputHashset(ent, output, args);
    }

    private void OnFireFilter(Entity<HereticRitualComponent> ent, ref FilterOnFireRitualEvent args)
    {
        if (!TryGetValue(ent, args.InputKey, out HashSet<EntityUid>? input))
        {
            args.Cancel();
            return;
        }

        HashSet<EntityUid> output = new();

        foreach (var uid in input)
        {
            if (_flammableQuery.TryComp(uid, out var flam) && flam.OnFire)
                output.Add(uid);
        }

        OutputHashset(ent, output, args);
    }

    private void OnObjectivesComlete(Entity<HereticRitualComponent> ent, ref ObjectivesCompleteRitualEvent args)
    {
        if (!_heretic.ObjectivesAllowAscension(args.Mind))
            args.Cancel();
    }

    private void OnCanAscend(Entity<HereticRitualComponent> ent, ref CanAscendRitualEvent args)
    {
        if (!args.Mind.Comp.CanAscend || args.Mind.Comp.Ascended)
            args.Cancel();
    }

    private void OnFindLimited(Entity<HereticRitualComponent> ent, ref FindLostLimitedOutputRitualEvent args)
    {
        if (ent.Comp.LimitedOutput.Count == 0)
        {
            args.Cancel();
            return;
        }

        var coords = _transform.GetMapCoordinates(args.Platform);
        EntityUid? selected = null;
        var maxDist = args.MinRange;

        foreach (var output in ent.Comp.LimitedOutput)
        {
            var outCoords = _transform.GetMapCoordinates(output);
            if (outCoords.MapId != coords.MapId)
            {
                selected = output;
                break;
            }

            var dist = (coords.Position - outCoords.Position).Length();

            if (dist < args.MinRange)
                continue;

            if (dist < maxDist)
                continue;

            maxDist = dist;
            selected = output;
        }

        if (selected is not { } uid)
        {
            if (args.CancelOnEmptyOutput)
                args.Cancel();
            return;
        }

        ent.Comp.Blackboard[args.OutputKey] = uid;
    }

    private void OnAddKnowledge(Entity<HereticRitualComponent> ent, ref AddKnowledgeRitualEvent args)
    {
        _heretic.TryAddKnowledge((args.Mind, null, args.Mind), args.Knowledge, args.Performer);
    }

    private void OnRaiseEvent(Entity<HereticRitualComponent> ent, ref RaiseHereticEventRitualEvent args)
    {
        RaiseLocalEvent(args.Mind, args.Event, true);
    }

    private void OnProcessIngredients(Entity<HereticRitualComponent> ent, ref ProcessIngredientsRitualEvent args)
    {
        if (!TryGetValue(ent, args.InputKey, out HashSet<EntityUid>? input))
        {
            args.Cancel();
            return;
        }

        var missingList = new Dictionary<LocId, int>();
        var toDelete = new List<EntityUid>();
        var toSplit = new List<(Entity<StackComponent> uid, int amount)>();

        var ingredientAmounts = Enumerable.Repeat(0, args.Ingredients.Count).ToList();

        foreach (var look in input)
        {
            for (var i = 0; i < args.Ingredients.Count; i++)
            {
                var ritIng = args.Ingredients[i];
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

        for (var i = 0; i < args.Ingredients.Count; i++)
        {
            var ritIng = args.Ingredients[i];
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

            return;
        }

        var sb = new StringBuilder();
        foreach (var (name, amount) in missingList)
        {
            sb.Append($"{Loc.GetString(name)} x{amount} ");
        }

        sb.Remove(sb.Length - 1, 1);

        args.CancelStringOverride = Loc.GetString("heretic-ritual-fail-items", ("itemlist", sb.ToString()));
        args.Cancel();
    }

    private void OnPathSpawn(Entity<HereticRitualComponent> ent, ref PathBasedSpawnRitualEvent args)
    {
        var coords = Transform(args.Platform).Coordinates;

        EntityUid spawned;
        if (args.Mind.Comp.CurrentPath is { } path && args.Output.TryGetValue(path, out var toSpawn))
            spawned = Spawn(toSpawn, coords);
        else
            spawned = Spawn(args.FallbackOutput, coords);

        if (ent.Comp.Limit <= 0)
            return;

        ent.Comp.LimitedOutput.Add(spawned);
    }

    private void OnSpawn(Entity<HereticRitualComponent> ent, ref SpawnRitualEvent args)
    {
        var coords = _transform.GetMapCoordinates(args.Platform);
        foreach (var (obj, amount) in args.Output)
        {
            for (var i = 0; i < amount; i++)
            {
                var spawned = Spawn(obj, coords);

                if (_ghoulQuery.HasComp(spawned))
                    _ghoul.SetBoundHeretic(spawned, args.Performer);

                if (ent.Comp.Limit <= 0)
                    continue;

                ent.Comp.LimitedOutput.Add(spawned);
                if (ent.Comp.LimitedOutput.Count >= ent.Comp.Limit)
                    break;
            }
        }
    }

    private void OnSacrifice(Entity<HereticRitualComponent> ent, ref SacrificeRitualEvent args)
    {
        if (!TryGetValue(ent, args.InputKey, out HashSet<EntityUid>? input) ||
            !TryComp(args.Mind, out MindComponent? mindComp) || !TryComp(args.Mind, out StoreComponent? store))
        {
            args.Cancel();
            return;
        }

        var knowledgeGain = 0f;
        foreach (var uid in input)
        {
            var isCommand = _commandQuery.HasComp(uid);
            var isSec = _securityQuery.HasComp(uid);
            var isHeretic = _heretic.TryGetHereticComponent(uid, out _, out _);
            knowledgeGain += isHeretic || IsSacrificeTarget(args.Mind, uid)
                ? isCommand || isSec || isHeretic ? 3f : 2f
                : 0f;

            _gibbing.Gib(uid);

            var ev = new IncrementHereticObjectiveProgressEvent(args.SacrificeObjective);
            RaiseLocalEvent(args.Mind, ref ev);

            if (!isCommand)
                continue;

            var ev2 = new IncrementHereticObjectiveProgressEvent(args.SacrificeHeadObjective);
            RaiseLocalEvent(args.Mind, ref ev2);
        }

        if (knowledgeGain > 0)
            _heretic.UpdateMindKnowledge((args.Mind, args.Mind, store, mindComp), args.Performer, knowledgeGain);
    }

    private void OnTakeNumber(Entity<HereticRitualComponent> ent, ref TakeNumberEntitiesRitualEvent args)
    {
        if (!TryGetValue(ent, args.InputKey, out HashSet<EntityUid>? input) || input.Count < args.Number)
        {
            args.Cancel();
            return;
        }

        var output = input.Take(args.Number).ToHashSet();

        OutputHashset(ent, output, args);
    }

    private void OnCombine(Entity<HereticRitualComponent> ent, ref CombineEntityHashSetRitualEvent args)
    {
        if (!TryGetValue(ent, args.InputKey, out HashSet<EntityUid>? input1) ||
            !TryGetValue(ent, args.InputKey2, out HashSet<EntityUid>? input2))
        {
            args.Cancel();
            return;
        }

        var output = input1.Concat(input2).ToHashSet();

        OutputHashset(ent, output, args);
    }

    private void OnTargetsFilter(Entity<HereticRitualComponent> ent, ref FilterTargetsRitualEvent args)
    {
        if (!TryGetValue(ent, args.InputKey, out HashSet<EntityUid>? input))
        {
            args.Cancel();
            return;
        }

        HashSet<EntityUid> output = new();
        foreach (var uid in input)
        {
            if (IsSacrificeTarget(args.Mind, uid))
                output.Add(uid);
        }

        OutputHashset(ent, output, args);
    }

    private void OnMobStateFilter(Entity<HereticRitualComponent> ent, ref FilterByMobStateRitualEvent args)
    {
        if (!TryGetValue(ent, args.InputKey, out HashSet<EntityUid>? input))
        {
            args.Cancel();
            return;
        }

        HashSet<EntityUid> output = new();
        foreach (var uid in input)
        {
            if (!_mobQuery.TryComp(uid, out var mob))
                continue;

            if ((mob.CurrentState == args.MobState) ^ args.Invert)
                output.Add(uid);
        }

        OutputHashset(ent, output, args);
    }

    private void OnFilter(Entity<HereticRitualComponent> ent, ref FilterRitualEvent args)
    {
        if (!TryGetValue(ent, args.InputKey, out HashSet<EntityUid>? input))
        {
            args.Cancel();
            return;
        }

        HashSet<EntityUid> output = new();
        foreach (var uid in input)
        {
            if (_whitelist.CheckBoth(uid, args.Blacklist, args.Whitelist))
                output.Add(uid);
        }

        OutputHashset(ent, output, args);
    }

    private void OnFilterHeretics(Entity<HereticRitualComponent> ent, ref FilterHereticsRitualEvent args)
    {
        if (!TryGetValue(ent, args.InputKey, out HashSet<EntityUid>? input))
        {
            args.Cancel();
            return;
        }

        HashSet<EntityUid> output = new();
        foreach (var uid in input)
        {
            if (_heretic.TryGetHereticComponent(uid, out _, out _))
                output.Add(uid);
        }

        OutputHashset(ent, output, args);
    }

    private void OnLookup(Entity<HereticRitualComponent> ent, ref LookupRitualEvent args)
    {
        var look = _lookup.GetEntitiesInRange(args.Platform, args.Range, args.Flags);
        OutputHashset(ent, look, args);
    }

    #endregion

    #region Helpers

    private bool IsSacrificeTarget(Entity<HereticComponent> heretic, EntityUid target)
    {
        return heretic.Comp.SacrificeTargets.Any(x => x.Entity == GetNetEntity(target));
    }

    private void OutputHashset(Entity<HereticRitualComponent> ent,
        HashSet<EntityUid> output,
        OutputHereticRitualEvent args)
    {
        if (args.CancelOnEmptyOutput && output.Count == 0)
        {
            args.Cancel();
            return;
        }

        ent.Comp.Blackboard[args.OutputKey] = output;
    }

    private bool TryGetValue<T>(Entity<HereticRitualComponent> ent, string key, [NotNullWhen(true)] out T? value)
    {
        if (ent.Comp.Blackboard.TryGetValue(key, out var val))
        {
            value = (T) val;
            return true;
        }

        value = default;
        return false;
    }

    public bool RaiseRitualEvents(Entity<HereticRitualComponent> ent,
        IEnumerable<BaseHereticRitualEvent> events,
        EntityUid performer,
        Entity<HereticComponent> mind,
        EntityUid platform)
    {
        foreach (var ev in events)
        {
            ev.Performer = performer;
            ev.Mind = mind;
            ev.Platform = platform;
            ev.CancelStringOverride = null;
            ev.Uncancel();
            RaiseLocalEvent(ent, (object) ev);
            if (!ev.Cancelled)
                continue;

            if (ev is RaiseRitualEventsRitualEvent)
                return false;

            var popup = ev.CancelStringOverride;
            if (popup == null && (ev.CancelLoc ?? ent.Comp.CancelLoc) is { } cancelLoc)
                popup = Loc.GetString(cancelLoc);
            if (popup != null)
                _popup.PopupEntity(popup, platform, performer);

            return false;
        }
        return true;
    }

    private bool TryDoRitual(Entity<HereticRitualComponent> ent,
        EntityUid performer,
        Entity<HereticComponent> mind,
        EntityUid platform)
    {
        if (ent.Comp.Limit > 0)
        {
            ent.Comp.LimitedOutput = ent.Comp.LimitedOutput.Where(Exists).ToList();
            if (ent.Comp.LimitedOutput.Count >= ent.Comp.Limit)
            {
                if (ent.Comp.LimitReachedEvents.Count > 0)
                {
                    return RaiseRitualEvents(ent, ent.Comp.LimitReachedEvents, performer, mind, platform);
                }

                _popup.PopupEntity(Loc.GetString("heretic-ritual-fail-limit"), platform, performer);
                return false;
            }
        }

        return RaiseRitualEvents(ent, ent.Comp.Events, performer, mind, platform);
    }

    #endregion

    #region RitualRuneEvents

    private void OnInteract(Entity<HereticRitualRuneComponent> ent, ref InteractHandEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.User, out var heretic, out _))
            return;

        if (heretic.Rituals.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ritual-norituals"), args.User, args.User);
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
        _popup.PopupEntity(Loc.GetString("heretic-ritual-switch", ("name", ritualName)), user, user);
    }

    private void OnInteractUsing(Entity<HereticRitualRuneComponent> ent, ref InteractUsingEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.User, out var heretic, out var mind))
            return;

        if (!HasComp<MansusGraspComponent>(args.Used))
            return;

        if (!TryComp(heretic.ChosenRitual, out HereticRitualComponent? ritual))
        {
            _popup.PopupEntity(Loc.GetString("heretic-ritual-noritual"), args.User, args.User);
            return;
        }

        ritual.Blackboard.Clear();

        if (TryDoRitual((heretic.ChosenRitual.Value, ritual), args.User, (mind, heretic), ent) &&
            ritual.PlaySuccessAnimation)
            RitualSuccess(ent, args.User);

        ritual.Blackboard.Clear();
    }

    private void OnExamine(Entity<HereticRitualRuneComponent> ent, ref ExaminedEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.Examiner, out var h, out _))
            return;

        var name = h.ChosenRitual != null ? Name(h.ChosenRitual.Value) : Loc.GetString("heretic-ritual-none");
        args.PushMarkup(Loc.GetString("heretic-ritualrune-examine", ("rit", name)));
    }

    public void RitualSuccess(EntityUid ent, EntityUid user)
    {
        _audio.PlayPvs(RitualSuccessSound, ent, AudioParams.Default.WithVolume(-3f));
        _popup.PopupEntity(Loc.GetString("heretic-ritual-success"), ent, user);
        Spawn("HereticRuneRitualAnimation", Transform(ent).Coordinates);
    }

    #endregion
}

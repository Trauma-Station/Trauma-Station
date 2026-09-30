// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.RoundEnd;
using Content.Server.StationEvents.Components;
using Content.Shared.Actions;
using Content.Shared.Antag;
using Content.Shared.Antag.Components;
using Content.Shared.Chat;
using Content.Shared.Cuffs.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Gibbing;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Roles;
using Content.Shared.Station.Systems;
using Content.Trauma.Server.BloodCult.Objectives;
using Content.Trauma.Shared.BloodCult;
using Content.Trauma.Shared.BloodCult.Components;
using Content.Trauma.Shared.BloodCult.Gamerule;
using Content.Trauma.Shared.BloodCult.Items;
using Content.Trauma.Shared.BloodCult.Runes.Offering;
using Content.Trauma.Shared.BloodCult.Runes.Rending;
using Content.Trauma.Shared.Roles;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.BloodCult.Gamerule;

public sealed partial class BloodCultRuleSystem : GameRuleSystem<BloodCultRuleComponent>
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private HumanoidProfileSystem _humanoid = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private EntityQuery<ActorComponent> _actorQuery = default!;
    [Dependency] private EntityQuery<BloodCultistComponent> _cultistQuery = default!;
    [Dependency] private EntityQuery<BloodCultLeaderComponent> _leaderQuery = default!;
    [Dependency] private EntityQuery<BloodCultRuleComponent> _query = default!;
    [Dependency] private EntityQuery<BloodCultTargetComponent> _targetQuery = default!;

    private static readonly Color AnnounceColor = Color.FromHex("#dc143c");

    private List<EntityUid> _targets = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        foreach (var ent in EntityQueryEnumerator<BloodCultTargetComponent>())
        {
            if (ent.Comp.NextExiled is not { } exiled || exiled < now)
                continue;

            var rule = ent.Comp.Rule;
            // target fucked off to lavaland cryod etc so pick a new one
            ent.Comp.Rule = EntityUid.Invalid;
            RemCompDeferred(ent, ent.Comp);

            if (_query.TryComp(rule, out var comp))
                PickTarget((rule, comp));
        }
    }

    protected override void Added(Entity<BloodCultRuleComponent, GameRuleComponent> ent, ref GameRuleAddedEvent args)
    {
        base.Added(ent, ref args);

        if (!_station.TryGetRandomStation(out var station))
        {
            Log.Error($"Ending {ToPrettyString(ent)} since there is no station");
            ForceEndSelf((ent, ent.Comp2));
            return;
        }

        ent.Comp1.Station = station.Value;
    }

    protected override void Started(Entity<BloodCultRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        var comp = ent.Comp1;
        PickTarget((ent, comp));
        if (comp.OfferingTarget == null)
        {
            Log.Warning($"Couldn't find an offering target, cult won't need it");
            comp.TargetSacrificed = true;
            DirtyField(ent, comp, nameof(BloodCultRuleComponent.TargetSacrificed));
        }

        while (comp.RitualAreas.Count < comp.AreaCount)
        {
            var area = _random.Pick(comp.AreaPool);
            // TODO: maybe verify that the map has the area, only really matters for reach but that shouldnt have a cult anyway
            if (!comp.RitualAreas.Contains(area))
                comp.RitualAreas.Add(area);
        }
        DirtyField(ent, comp, nameof(BloodCultRuleComponent.RitualAreas));
    }

    protected override void AppendRoundEndText(Entity<BloodCultRuleComponent> ent, ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(ent, ref args);

        var (uid, comp) = ent;
        var winText = Loc.GetString($"blood-cult-condition-{comp.WinCondition.ToString().ToLower()}");
        args.AddLine(winText);

        args.AddLine(Loc.GetString("blood-cultists-list-start"));

        var sessionData = _antag.GetAntagIdentifiers(uid);
        foreach (var (_, data, name) in sessionData)
        {
            var lising = Loc.GetString("blood-cultists-list-name", ("name", name), ("user", data.UserName));
            args.AddLine(lising);
        }
    }

    [SubscribeLocalEvent]
    private void OnSacrificed(ref BloodCultSacrificedEvent args)
    {
        var rule = args.Rule;
        if (args.Target != rule.Comp.OfferingTarget)
            return;

        rule.Comp.TargetSacrificed = true;
        DirtyField(rule, rule.Comp, nameof(BloodCultRuleComponent.TargetSacrificed));
        AnnounceToCult(rule, "Nar'Sie's target has been sacrificed! Summon her to your realm and elevate your cult to godhood!");
    }

    [SubscribeLocalEvent]
    private void OnNarsieSummoned(ref BloodCultNarsieSummonedEvent args)
    {
        if (_cult.GetRule(args.User) is not { } rule)
        {
            Log.Error($"Nar'Sie was summoned by a non-cultist {ToPrettyString(args.User)}!?");
            _roundEnd.EndRound();
            return;
        }

        rule.Comp.NarSieSummoned = true;
        DirtyField(rule, rule.Comp, nameof(BloodCultRuleComponent.NarSieSummoned));
        rule.Comp.WinCondition = CultWinCondition.Win;
        _roundEnd.EndRound();

        foreach (var ent in rule.Comp.Cultists)
        {
            if (Deleted(ent))
                continue;

            var harvester = Spawn(rule.Comp.HarvesterPrototype, Transform(ent).Coordinates);
            _cult.CopyMember(ent, harvester);
            if (_mind.GetMind(ent) is { } mind)
                _mind.TransferTo(mind, harvester);
            _gibbing.Gib(ent);
        }
    }

    [SubscribeLocalEvent]
    private void OnAntagEntitySelected(Entity<BloodCultRuleComponent> rule, ref AfterAntagEntitySelectedEvent args)
    {
        var mob = args.EntityUid;
        _cult.SetCultRule(mob, rule);
        rule.Comp.Cultists.Add(mob);
        UpdateCultStage(rule.Comp);

        // W pvs
        var query = EntityQueryEnumerator<BloodCultMemberComponent>();
        foreach (var ent in query)
        {
            if (ent.Comp.Rule != rule.Owner)
                continue;

            Dirty(ent);

            if (!_cultistQuery.TryComp(ent, out var cultist))
                continue;

            Dirty(ent, cultist);

            if (_leaderQuery.TryComp(ent, out var leader))
                Dirty(ent, leader);
        }
    }

    [SubscribeLocalEvent]
    private void OnCultistComponentRemoved(Entity<BloodCultistComponent> cultist, ref ComponentRemove args)
    {
        if (_cult.GetRule(cultist) is { } rule)
        {
            rule.Comp.Cultists.Remove(cultist);
        }

        CheckRoundShouldEnd();

        if (TerminatingOrDeleted(cultist.Owner))
            return;

        RemoveAllCultItems(cultist);
        RemoveCultistAppearance(cultist.AsNullable());

        if (_cult.GetSpells(cultist) is { } spells)
        {
            // this should happen anyway from mind events but just incase
            RemComp(spells, spells.Comp);
        }
    }

    [SubscribeLocalEvent]
    private void OnCultistsStateChanged(Entity<BloodCultistComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            CheckRoundShouldEnd();
    }

    [SubscribeLocalEvent]
    private void OnTargetShutdown(Entity<BloodCultTargetComponent> ent, ref ComponentShutdown args)
    {
        var rule = ent.Comp.Rule;
        if (TerminatingOrDeleted(rule) || !_query.TryComp(rule, out var comp))
            return;

        ent.Comp.Rule = EntityUid.Invalid;

        // gibbed or something pick a new target if it wasn't just sacrificed
        PickTarget((rule, comp));
    }

    [SubscribeLocalEvent]
    private void OnTargetGridChanged(Entity<BloodCultTargetComponent> ent, ref GridUidChangedEvent args)
    {
        var rule = ent.Comp.Rule;
        if (TerminatingOrDeleted(ent) || !_query.TryComp(rule, out var comp))
            return; // shutdown will handle it when detaching

        var station = _station.GetOwningStation(args.NewGrid);
        ent.Comp.NextExiled = station == comp.Station
            ? null // still on station don't care
            : _timing.CurTime + ent.Comp.ExileTime;
    }

    public bool Convert(EntityUid rule, EntityUid target, [ForbidLiteral] ProtoId<AntagSpecifierPrototype> specifier)
    {
        if (!TryComp<AntagSelectionComponent>(rule, out var antag))
        {
            Log.Error($"Bad gamerule {ToPrettyString(rule)} was missing AntagSelectionComponent!");
            return false;
        }

        if (!_actorQuery.TryComp(target, out var actor))
            return false;

        var antagEnt = (rule, antag);
        return _antag.TryMakeAntag(antagEnt, specifier, actor.PlayerSession, checkPref: false);
    }

    private void CheckRoundShouldEnd()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var cult, out _, out _))
        {
            var aliveCultists = cult.Cultists.Count(cultist => !_mob.IsDead(cultist));
            if (aliveCultists != 0)
                continue;

            cult.WinCondition = CultWinCondition.Failure;

            // Check for all at once gamemode
            if (!GameTicker.GetActiveGameRules().Where(HasComp<RampingStationEventSchedulerComponent>).Any())
                _roundEnd.EndRound();
        }
    }

    private void PickTarget(Entity<BloodCultRuleComponent> rule)
    {
        var comp = rule.Comp;
        comp.OfferingTarget = comp.TargetSacrificed
            ? null // done the job
            : PickTarget(comp.Station);

        if (comp.OfferingTarget is { } target)
        {
            AnnounceToCult(rule, $"Nar'Sie demands the sacrifice of {Name(target)}!");
            var ev = new CultTargetAssignedEvent(rule, target);
            RaiseLocalEvent(ref ev);
        }
    }

    private EntityUid? PickTarget(EntityUid station)
    {
        _targets.Clear();
        // TODO: use mind pools, prioritize command and sec before literally everyone
        var query = EntityQueryEnumerator<ActorComponent, HumanoidProfileComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out _, out _, out var mc))
        {
            // never include cultists as targets or people off station
            if (mc.Mind is not { } mind || _cult.IsMindCultist(mind) || _station.GetOwningStation(uid) != station)
                continue;

            // multiple cults cant target the same guy
            if (_targetQuery.HasComp(uid))
                continue;

            _targets.Add(uid);
        }

        return _targets.Count > 0 ? _random.Pick(_targets) : null;
    }

    public void AnnounceToCult(Entity<BloodCultRuleComponent> rule, string message)
    {
        var clients = new List<INetChannel>();
        foreach (var cultist in rule.Comp.Cultists)
        {
            if (_actorQuery.TryComp(cultist, out var actor))
                clients.Add(actor.PlayerSession.Channel);
        }
        var channel = ChatChannel.Server;
        _chat.ChatMessageToMany(channel, message, message, rule, false, true, clients, AnnounceColor);
    }

    private void RemoveAllCultItems(Entity<BloodCultistComponent> cultist)
    {
        if (!_inventory.TryGetContainerSlotEnumerator(cultist.Owner, out var enumerator))
            return;

        while (enumerator.MoveNext(out var container))
        {
            if (container.ContainedEntity != null && HasComp<CultItemComponent>(container.ContainedEntity.Value))
                _container.Remove(container.ContainedEntity.Value, container, true, true);
        }

        foreach (var item in _hands.EnumerateHeld(cultist.Owner))
        {
            if (TryComp(item, out CultItemComponent? cultItem) && !cultItem.AllowUseToEveryone &&
                !_hands.TryDrop(cultist.Owner, item, null, false, false))
                QueueDel(item);
        }
    }

    private void RemoveCultistAppearance(Entity<BloodCultistComponent?> cultist)
    {
        if (!Resolve(cultist, ref cultist.Comp))
            return;

        _humanoid.SetEyeColor(cultist, cultist.Comp.OriginalEyeColor);
        RemComp<PentagramComponent>(cultist);
    }

    private void UpdateCultStage(BloodCultRuleComponent cultRule)
    {
        var cultistsCount = cultRule.Cultists.Count;
        var prevStage = cultRule.Stage;

        if (cultistsCount >= cultRule.PentagramThreshold)
        {
            cultRule.Stage = CultStage.Pentagram;
            SelectRandomLeader(cultRule);
        }
        else if (cultistsCount >= cultRule.ReadEyeThreshold)
            cultRule.Stage = CultStage.RedEyes;
        else
            cultRule.Stage = CultStage.Start;

        if (cultRule.Stage != prevStage)
            UpdateCultistsAppearance(cultRule, prevStage);
    }

    private void UpdateCultistsAppearance(BloodCultRuleComponent cultRule, CultStage prevStage)
    {
        switch (cultRule.Stage)
        {
            case CultStage.Start when prevStage == CultStage.RedEyes:
                foreach (var cultist in cultRule.Cultists)
                    RemoveCultistAppearance(cultist);

                break;
            case CultStage.RedEyes when prevStage == CultStage.Start:
                foreach (var uid in cultRule.Cultists)
                {
                    if (!TryComp<BloodCultistComponent>(uid, out var cultist))
                        continue;
                    if (_humanoid.GetEyeColor(uid) is { } eyeColor)
                        cultist.OriginalEyeColor = eyeColor;
                    _humanoid.SetEyeColor(uid, cultRule.EyeColor);
                }

                break;
            case CultStage.Pentagram:
                foreach (var cultist in cultRule.Cultists)
                    EnsureComp<PentagramComponent>(cultist);

                break;
        }
    }

    /// <summary>
    ///     A crutch while we have no NORMAL voting system. The DarkRP one fucking sucks.
    /// </summary>
    private void SelectRandomLeader(BloodCultRuleComponent cultRule)
    {
        if (cultRule.LeaderSelected)
            return;

        var candidates = new List<EntityUid>(cultRule.Cultists);
        candidates.RemoveAll(
            entity =>
                TryComp(entity, out PullableComponent? pullable) && pullable.BeingPulled ||
                TryComp(entity, out CuffableComponent? cuffable) && cuffable.CuffedHandCount > 0);

        if (candidates.Count == 0)
            return;

        var leader = _random.Pick(candidates);
        AddComp<BloodCultLeaderComponent>(leader);
        cultRule.LeaderSelected = true;
        cultRule.CultLeader = leader;
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Server.StationEvents.Components;
using Content.Shared.Actions;
using Content.Shared.Antag;
using Content.Shared.Chat;
using Content.Shared.Cuffs.Components;
using Content.Shared.GameTicking.Components;
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

namespace Content.Trauma.Server.BloodCult.Gamerule;

public sealed partial class BloodCultRuleSystem : GameRuleSystem<BloodCultRuleComponent>
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private HumanoidProfileSystem _humanoid = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private EntityQuery<ActorComponent> _actorQuery = default!;

    private static readonly Color AnnounceColor = Color.FromHex("#dc143c");

    private List<EntityUid> _targets = new();

    // TODO: make a thing so if target cryos it picks a new one

    protected override void Started(
        EntityUid uid,
        BloodCultRuleComponent comp,
        GameRuleComponent rule,
        GameRuleStartedEvent args
    )
    {
        base.Started(uid, comp, rule, args);

        comp.OfferingTarget = PickTarget();
        while (comp.RitualAreas.Count < comp.AreaCount)
        {
            var area = _random.Pick(comp.AreaPool);
            // TODO: maybe verify that the map has the area, only really matters for reach but that shouldnt have a cult anyway
            if (!comp.RitualAreas.Contains(area))
                comp.RitualAreas.Add(area);
        }
        DirtyField(uid, comp, nameof(BloodCultRuleComponent.RitualAreas));
    }

    protected override void AppendRoundEndText(
        EntityUid uid,
        BloodCultRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args
    )
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var winText = Loc.GetString($"blood-cult-condition-{component.WinCondition.ToString().ToLower()}");
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

    public void Convert(EntityUid rule, EntityUid target, [ForbidLiteral] ProtoId<AntagSpecifierPrototype> specifier)
    {
        if (!TryComp<AntagSelectionComponent>(rule, out var antag) ||
            !TryComp<ActorComponent>(target, out var actor))
            return;

        var antagEnt = (rule, antag);
        _antag.TryMakeAntag(antagEnt, specifier, actor.PlayerSession);
    }

    private void CheckRoundShouldEnd()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var cult, out _))
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

    private EntityUid? PickTarget()
    {
        _targets.Clear();
        // TODO: use mind pools, prioritize command and sec before literally everyone
        var query = EntityQueryEnumerator<ActorComponent, HumanoidProfileComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out _, out _, out var mc))
        {
            // never include cultsits as targets
            if (mc.Mind is not { } mind || _cult.IsMindCultist(mind))
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

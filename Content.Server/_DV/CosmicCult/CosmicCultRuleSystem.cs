using Content.Goobstation.Common.Religion;
using Content.Goobstation.Common.Temperature.Components;
using Content.Goobstation.Shared.Religion; // Goobstation - Shitchap
using Content.Goobstation.Shared.Religion.Nullrod;
using Content.Server._DV.CosmicCult.Components;
using Content.Server.Actions;
using Content.Shared.Administration.Systems;
using Content.Server.Antag;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Rotting;
using Content.Server.Audio;
using Content.Server.Cuffs;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.EUI;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Objectives.Components;
using Content.Shared.Radio.Components;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Shared.Eye;
using Content.Shared._DV.CosmicCult.Components.Examine;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared._DV.CosmicCult.Prototypes;
using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.Roles;
using Content.Shared.Audio;
using Content.Shared.Cuffs.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Gibbing;
using Content.Shared.Humanoid;
using Content.Shared.Light.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Server.Audio;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server._DV.CosmicCult;

/// <summary>
/// Where all the main stuff for Cosmic Cultists happens.
/// </summary>
public sealed class CosmicCultRuleSystem : GameRuleSystem<CosmicCultRuleComponent>
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergency = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly CuffableSystem _cuffable = default!; // goob edit
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly RottingSystem _rotting = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly SoundSpecifier _briefingSound = new SoundPathSpecifier("/Audio/_DV/CosmicCult/antag_cosmic_briefing.ogg");
    private readonly SoundSpecifier _deconvertSound = new SoundPathSpecifier("/Audio/_DV/CosmicCult/antag_cosmic_deconvert.ogg");
    private readonly SoundSpecifier _tier3Sound = new SoundPathSpecifier("/Audio/_DV/CosmicCult/tier3.ogg");
    private readonly SoundSpecifier _tier2Sound = new SoundPathSpecifier("/Audio/_DV/CosmicCult/tier2.ogg");
    private readonly SoundSpecifier _tier1Sound = new SoundPathSpecifier("/Audio/_DV/CosmicCult/tier1.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<CosmicCultAssociateRuleEvent>(OnAssociateRule);
        SubscribeLocalEvent<CosmicCultRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelect);
        SubscribeLocalEvent<CosmicCultComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<CosmicLesserCultistComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<CosmicGodComponent, ComponentInit>(OnGodSpawn);
        SubscribeLocalEvent<CosmicCultComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<CommunicationConsoleCallShuttleAttemptEvent>(OnEvacAttempt);
    }

    #region Starting Events

    protected override void ActiveTick(EntityUid uid, CosmicCultRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (component.ExtraRiftTimer is { } riftTimer && _timing.CurTime >= riftTimer && !component.RiftStop)
        {
            component.ExtraRiftTimer = _timing.CurTime + _rand.Next(TimeSpan.FromSeconds(230), TimeSpan.FromSeconds(360)); //3min50 to 6min between new rifts. Seconds instead of minutes for granularity.
            SpawnRift(component.FractureChance);
        }
        if (component.UpdateAllCultists)
        {
            component.UpdateAllCultists = false;
            UpdateCultData((uid, component));
        }
    }

    private void OnAntagSelect(Entity<CosmicCultRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        ent.Comp.UpdateAllCultists = true; // Update all the numbers at the next tick, when all the cultist roles are (hopefuly) already assigned
        TryStartCult(args.EntityUid, ent);
    }

    /// <summary>
    /// Spawns a malign rift in a random spot on the station.
    /// </summary>
    /// <param name="fractureChance">The chance that a malign fracture will spawn instead.</param>
    public void SpawnRift(float fractureChance = 0)
    {
        if (TryFindRandomTile(out var _, out var _, out var _, out var coords))
        {
            Spawn(_rand.Prob(fractureChance) ? "CosmicGreaterMalignRift" : "CosmicMalignRift", coords);
        }
    }

    public void UpdateCultData(Entity<CosmicCultComponent> ent)
    {
        if (AssociatedGamerule(ent) is { } cult) UpdateCultData(cult);
    }

    /// <summary>
    /// Recalculates some values for a given cult gamerule.
    /// </summary>
    private void UpdateCultData(Entity<CosmicCultRuleComponent> rule)
    {
        var totalCult = 0;
        var cultistsAtNextLevel = 0;
        foreach (var cultist in rule.Comp.Cultists)
        {
            if (!TryComp<CosmicCultComponent>(cultist, out var comp)) return;
            if (comp.CurrentLevel > rule.Comp.CurrentTier + 1) // Someone is speedrunning, increase cult level immediately
            {
                IncreaseCultTier(rule);
                return;
            }
            if (!_mobState.IsDead(cultist)) totalCult++; // Dead cultists don't count towards progression reqs to encourage sec to not just DNR captured cultists
            if (comp.CurrentLevel > rule.Comp.CurrentTier) cultistsAtNextLevel++;
        }

        rule.Comp.CultistsForNextTier = (int) Math.Ceiling(totalCult / 2f);

        foreach (var cultist in rule.Comp.Cultists)
        {
            if (!TryComp<CosmicCultComponent>(cultist, out var comp)) return;
            comp.CultTier = rule.Comp.CurrentTier;
            comp.CultistsForNextLevel = rule.Comp.CurrentTier >= comp.MaxLevel ? 0 : rule.Comp.CultistsForNextTier - cultistsAtNextLevel;
            Dirty(cultist, comp);
            if (comp.CosmicShopActionEntity is not { } shop) return;
            _ui.SetUiState(shop, CosmicShopKey.Key, new CosmicShopBuiState());
        }

        rule.Comp.TotalCrew = _player.Sessions.Count(session
            => session.Status == SessionStatus.InGame
                && HasComp<HumanoidProfileComponent>(session.AttachedEntity));

        rule.Comp.PercentConverted = Math.Round((double) (100 * totalCult) / rule.Comp.TotalCrew);

        if (cultistsAtNextLevel >= rule.Comp.CultistsForNextTier && !rule.Comp.IncreasingTier)
            IncreaseCultTier(rule);

        rule.Comp.TotalCult = totalCult;
        rule.Comp.CultistsAtNextLevel = cultistsAtNextLevel;
    }

    /// <summary>
    /// Moves the cosmic cult to a next tier.
    /// </summary>
    private void IncreaseCultTier(Entity<CosmicCultRuleComponent> ent)
    {
        ent.Comp.IncreasingTier = true; // don't recurse, prevent infinite UpdateCultData-IncreaseCultTier loop
        ent.Comp.CurrentTier++;
        var component = ent.Comp;
        var lights = EntityQueryEnumerator<PoweredLightComponent>();
        switch (ent.Comp.CurrentTier)
        {
            case 1:
                _chatSystem.DispatchGlobalAnnouncement(
                    Loc.GetString("cosmiccult-announce-tier1-warning"),
                    sender: null,
                    true,
                    _tier1Sound,
                    Color.FromHex("#cae8e8"));

                for (var i = 0; i <= component.TotalCrew / 6; i++)
                    SpawnRift(component.FractureChance); // FractureChance should be 0 at this point but we'll pass it just in case

                while (lights.MoveNext(out var light, out _))
                    if (_rand.Prob(0.30f))
                        _ghost.DoGhostBooEvent(light);

                break;

            case 2:
                _chatSystem.DispatchGlobalAnnouncement(
                    Loc.GetString("cosmiccult-announce-tier2-warning"),
                    sender: null,
                    true,
                    _tier2Sound,
                    Color.FromHex("#cae8e8"));

                component.FractureChance = 0.75f; // Most rifts from this point will be fractures instead.

                for (var i = 0; i <= component.TotalCrew / 4; i++)
                    SpawnRift(component.FractureChance);

                while (lights.MoveNext(out var light, out _))
                    if (_rand.Prob(0.60f))
                        _ghost.DoGhostBooEvent(light);

                break;

            case 3:
                var sender = Loc.GetString("cosmiccult-announcement-sender");
                _chatSystem.DispatchGlobalAnnouncement(
                    Loc.GetString("cosmiccult-announce-tier3-fluff"),
                    sender,
                    false,
                    null,
                    Color.FromHex("#4cabb3"));
                _chatSystem.DispatchGlobalAnnouncement(
                    Loc.GetString("cosmiccult-announce-tier3-warning"),
                    null,
                    false,
                    null,
                    Color.FromHex("#cae8e8"));
                _audio.PlayGlobal(_tier3Sound, Filter.Broadcast(), false, AudioParams.Default);

                while (lights.MoveNext(out var light, out _))
                    _ghost.DoGhostBooEvent(light);

                break;
            default:
                throw new ArgumentException("Cosmic cult rule progressed to a stage with no defined behaviour");
        }
        UpdateCultData(ent); // Update all the data again
        ent.Comp.IncreasingTier = false;
    }

    #endregion

    #region Round & Objectives
    private void OnGodSpawn(Entity<CosmicGodComponent> ent, ref ComponentInit args)
    {
        var query = QueryActiveRules();
        _sound.StopStationEventMusic(ent, StationEventMusicType.CosmicCult);

        while (query.MoveNext(out var ruleUid, out _, out var cultRule, out _))
        {
            SetWinType((ruleUid, cultRule), WinType.CultMajor); //here's no coming back from this. Cult wins this round
            _roundEnd.EndRound(); //Woo game over yeaaaah
            foreach (var cultist in cultRule.Cultists)
            {
                if (!TryComp(cultist, out MobStateComponent? state)
                    || state.CurrentState == MobState.Dead
                    || !TryComp(cultist, out MindContainerComponent? mindContainer)
                    || !mindContainer.HasMind)
                    continue;

                var ascendant = Spawn("MobCosmicAstralAscended", Transform(cultist).Coordinates);
                _mind.TransferTo(mindContainer.Mind.Value, ascendant);
                _metaData.SetEntityName(ascendant, Loc.GetString("cosmiccult-astral-ascendant", ("name", cultist))); //Renames cultists' ascendant forms to "[CharacterName], Ascendant"
                _gibbing.Gib(cultist); // you don't need that body anymore
            }

            QueueDel(cultRule.MonumentInGame); // The monument doesn't need to stick around postround! Into the bin with you.
        }
    }

    private static void SetWinType(Entity<CosmicCultRuleComponent> ent, WinType type)
    {
        if (ent.Comp.WinLocked)
            return;

        ent.Comp.WinType = type;

        if (type is WinType.CultMajor) //Let's lock in our WinType to prevent us from setting a worse win if a better win's been achieved.
            ent.Comp.WinLocked = true;
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New is not GameRunLevel.PostRound) //Are we moving to post-round?
            return;

        var query = QueryActiveRules();

        while (query.MoveNext(out var uid, out _, out var cultRule, out _))
            ConfirmWinState((uid, cultRule)); //If so, let's consult our Winconditions and set an appropriate WinType.
    }

    private bool CultistsAlive()
    {
        var query = EntityQueryEnumerator<CosmicCultComponent, MobStateComponent>();
        while (query.MoveNext(out var ent, out var comp, out var mob)) // goob edit
        {

            if (TryComp<CuffableComponent>(ent, out var cuffComp) && _cuffable.IsCuffed((ent, cuffComp))) // goob edit
                continue; // dont count restrained cultists as counting towards objectives.

            if (!mob.Running
                || mob.CurrentState != MobState.Alive)
                continue;

            return true;
        }

        return false;
    }

    private void OnMobStateChanged(Entity<CosmicCultComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateCultData(ent); // Dead cultists don't count for levelup, recalculate the requirements

        if (CultistsAlive())
            return;

        var query = QueryActiveRules(); // Everyone is dead or captured, call evac
        while (query.MoveNext(out var ruleUid, out _, out var ruleComp, out _))
        {
            ConfirmWinState((ruleUid, ruleComp));

            _roundEnd.DoRoundEndBehavior(ruleComp.RoundEndBehavior,
                ruleComp.EvacShuttleTime,
                ruleComp.RoundEndTextSender,
                ruleComp.RoundEndTextShuttleCall,
                ruleComp.RoundEndTextAnnouncement);

            ruleComp.RoundEndBehavior = RoundEndBehavior.Nothing; // prevent this being called multiple times.
            ruleComp.RiftStop = true; // rifts can stop spawning now.
        }
    }

    private void OnEvacAttempt(ref CommunicationConsoleCallShuttleAttemptEvent args)
    {
        var query = EntityQueryEnumerator<MonumentComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            if (comp.Active)
            {
                args.Cancelled = true;
            }
        }
    }

    // TODO: rewrite this fuck
    private void ConfirmWinState(Entity<CosmicCultRuleComponent> ent)
    {
        _sound.StopStationEventMusic(ent, StationEventMusicType.CosmicCult);
        if (ent.Comp.WinLocked) return;

        var arrestedCultists = 0; // Cultists that are cuffed and at centcomm
        var deadCultists = 0; // Cultists that are dead (or crit) and NOT at centcomm
        var freeCultists = 0; // Cultists that are alive and unrestrained
        var halfCult = (int) Math.Ceiling(ent.Comp.InitialCult / 2f); // Half of the initial cult, rounded up
        var totalCult = ent.Comp.InitialCult;
        var centcomm = _emergency.GetCentcommMaps();
        var wrapup = AllEntityQuery<CosmicCultComponent, TransformComponent>();
        while (wrapup.MoveNext(out var cultist, out _, out var cultistLocation))
        {
            if (cultistLocation.MapUid == null) continue;
            if (centcomm.Contains(cultistLocation.MapUid.Value)
            && TryComp<CuffableComponent>(ent, out var cuffComp)
            && _cuffable.IsCuffed((ent, cuffComp)))
            {
                arrestedCultists++;
            }
            else if (_mobState.IsIncapacitated(cultist))
            {
                deadCultists++;
            }
            else
            {
                freeCultists++;
            }
        }
        if (arrestedCultists >= totalCult)
            SetWinType(ent, WinType.CrewMajor);
        else if (arrestedCultists >= halfCult)
            SetWinType(ent, WinType.CrewMinor);
        else if (deadCultists + arrestedCultists >= halfCult)
            SetWinType(ent, WinType.Neutral);
        else
            SetWinType(ent, WinType.CultMinor);
    }

    protected override void AppendRoundEndText(EntityUid uid,
        CosmicCultRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        var ftlKey = component.WinType.ToString().ToLower();
        var winType = Loc.GetString($"cosmiccult-roundend-{ftlKey}");
        var summaryText = Loc.GetString($"cosmiccult-summary-{ftlKey}");
        args.AddLine(winType);
        args.AddLine(summaryText);
        args.AddLine(Loc.GetString("cosmiccult-roundend-cultist-count", ("initialCount", component.InitialCult)));
        args.AddLine(Loc.GetString("cosmiccult-roundend-entropy-count", ("count", component.EntropySiphoned)));
        args.AddLine(Loc.GetString("cosmiccult-roundend-list-start"));

        var antags = _antag.GetAntagIdentifiers(uid);

        foreach (var (mind, sessionData, name) in antags)
        {
            if (!_role.MindHasRole<CosmicCultRoleComponent>(mind, out _)) continue;
            args.AddLine(Loc.GetString("cosmiccult-roundend-list-name-user", ("name", name), ("user", sessionData.UserName)));
        }
    }

    public void IncrementCultObjectiveEntropy(Entity<CosmicCultComponent> ent, int amount)
    {
        if (AssociatedGamerule(ent) is not { } cult)
            return;

        cult.Comp.EntropySiphoned += amount;
        var query = EntityQueryEnumerator<CosmicEntropyConditionComponent>();

        while (query.MoveNext(out _, out var entropyComp))
            entropyComp.Siphoned = cult.Comp.EntropySiphoned;
    }
    #endregion

    #region De- & Conversion
    public void TryStartCult(EntityUid uid, Entity<CosmicCultRuleComponent> rule)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;

        EnsureComp<CosmicCultComponent>(uid, out var cultComp);
        EnsureComp<IntrinsicRadioReceiverComponent>(uid);
        EnsureComp<CosmicCultAssociatedRuleComponent>(uid, out var associatedComp);

        foreach (var influenceProto in _proto.EnumeratePrototypes<InfluencePrototype>().Where(influenceProto => influenceProto.Tier == cultComp.CurrentLevel))
            cultComp.UnlockedInfluences.Add(influenceProto.ID);

        associatedComp.CultGamerule = rule;

        if (!_role.MindHasRole<CosmicCultRoleComponent>(mindId, out _))
            _role.MindAddRole(mindId, "MindRoleCosmicCult", mind, true); // It applies twice for some reason?
        _role.MindHasRole<CosmicCultRoleComponent>(mindId, out var cosmicRole);

        _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-roundstart-fluff"), Color.FromHex("#4cabb3"), _briefingSound);
        _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-short-briefing"), Color.FromHex("#cae8e8"), null);

        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(uid);
        var radio = EnsureComp<ActiveRadioComponent>(uid);
        radio.Channels.Add("CosmicRadio");
        transmitter.Channels.Add("CosmicRadio");

        if (_player.TryGetSessionById(mind.UserId, out var session))
        {
            _euiMan.OpenEui(new CosmicRoundStartEui(), session);
        }

        cultComp.WasWeakToHoly = HasComp<WeakToHolyComponent>(uid);
        if (!cultComp.WasWeakToHoly)
        {
            EnsureComp<WeakToHolyComponent>(uid);

            var ev = new UnholyStatusChangedEvent(uid, uid, true);
            RaiseLocalEvent(uid, ref ev);
        }

        rule.Comp.TotalCult++;

        Dirty(uid, cultComp);

        rule.Comp.Cultists.Add(uid);
        rule.Comp.InitialCult++;
    }

    private void OnAssociateRule(ref CosmicCultAssociateRuleEvent args)
    {
        TransferCultAssociation(args.Originator, args.Target);
    }

    public void TransferCultAssociation(EntityUid from, EntityUid to)
    {
        if (!TryComp<CosmicCultAssociatedRuleComponent>(from, out var source))
            return;

        var destination = EnsureComp<CosmicCultAssociatedRuleComponent>(to);
        destination.CultGamerule = source.CultGamerule;
    }

    public Entity<CosmicCultRuleComponent>? AssociatedGamerule(EntityUid uid)
    {
        if (!TryComp<CosmicCultAssociatedRuleComponent>(uid, out var associated))
        {
            Log.Debug("{0} has no associated rule", uid);
            return null;
        }

        if (!TryComp<CosmicCultRuleComponent>(associated.CultGamerule, out var cult))
        {
            Log.Debug("Associated gamerule {0} is not a cult gamerule", associated.CultGamerule);
            return null;
        }

        return (associated.CultGamerule, cult);
    }

    public void CosmicConversion(EntityUid converter, EntityUid uid)
    {
        if (AssociatedGamerule(converter) is not { } cult
        || !_mind.TryGetMind(uid, out var mindId, out var mind)
        || HasComp<MindShieldComponent>(uid)
        || HasComp<BibleUserComponent>(uid)
        || _rotting.IsRotten(uid))
            return;

        _rejuvenate.PerformRejuvenate(uid);

        _role.MindAddRole(mindId, "MindRoleCosmicCult", mind, true);
        _role.MindHasRole<CosmicCultRoleComponent>(mindId, out var cosmicRole);

        if (!_player.TryGetSessionById(mind.UserId, out var session))
            return;

        _antag.SendBriefing(session, Loc.GetString("cosmiccult-role-conversion-fluff"), Color.FromHex("#4cabb3"), _briefingSound);
        _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-conversion-briefing"), Color.FromHex("#cae8e8"), null);

        var cultComp = EnsureComp<CosmicLesserCultistComponent>(uid);
        TransferCultAssociation(converter, uid);
        Dirty(uid, cultComp);

        EnsureComp<CosmicSubtleMarkComponent>(uid);
        EnsureComp<PressureImmunityComponent>(uid);
        EnsureComp<SpecialLowTempImmunityComponent>(uid);
        EnsureComp<CosmicNonRespiratingComponent>(uid);

        cultComp.WasWeakToHoly = HasComp<WeakToHolyComponent>(uid);
        if (!cultComp.WasWeakToHoly)
        {
            EnsureComp<WeakToHolyComponent>(uid);

            var ev = new UnholyStatusChangedEvent(uid, uid, true);
            RaiseLocalEvent(uid, ref ev);
        }

        EnsureComp<IntrinsicRadioReceiverComponent>(uid);
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(uid);
        var radio = EnsureComp<ActiveRadioComponent>(uid);
        radio.Channels = ["CosmicRadio"];
        transmitter.Channels = ["CosmicRadio"];

        cultComp.DamageTransferActionEntity = _actions.AddAction(uid, cultComp.DamageTransferAction);

        if (TryComp(uid, out EyeComponent? eyeComp))
            _eye.SetVisibilityMask(uid, eyeComp.VisibilityMask | (int) VisibilityFlags.CosmicCultMonument);

        _mind.TryAddObjective(mindId, mind, "CosmicFinalityObjective");
        _mind.TryAddObjective(mindId, mind, "CosmicMonumentObjective");

        _euiMan.OpenEui(new CosmicConvertedEui(), session);
    }

    private void OnComponentShutdown(Entity<CosmicCultComponent> ent, ref ComponentShutdown args)
    {
        if (AssociatedGamerule(ent) is not { } cult)
            return;

        cult.Comp.InitialCult--; // This should only really happen if the cultist is deleted somehow, so we don't count them anymore.

        if (TerminatingOrDeleted(ent))
            return;
        var cosmicGamerule = cult.Comp;

        foreach (var actionEnt in ent.Comp.ActionEntities) _actions.RemoveAction(actionEnt);

        if (TryComp<IntrinsicRadioTransmitterComponent>(ent, out var transmitter))
            transmitter.Channels.Remove("CosmicRadio");

        if (TryComp<ActiveRadioComponent>(ent, out var radio))
            radio.Channels.Remove("CosmicRadio");

        RemComp<InfluenceVitalityComponent>(ent);
        RemComp<InfluenceStrideComponent>(ent);
        RemComp<PressureImmunityComponent>(ent);
        RemComp<SpecialLowTempImmunityComponent>(ent);
        RemComp<CosmicNonRespiratingComponent>(ent);
        RemComp<CosmicStarMarkComponent>(ent);
        RemComp<CosmicSubtleMarkComponent>(ent);

        var ev = new UnholyStatusChangedEvent(ent, ent, false);
        RaiseLocalEvent(ent, ref ev);

        _antag.SendBriefing(ent, Loc.GetString("cosmiccult-role-deconverted-fluff"), Color.FromHex("#4cabb3"), _deconvertSound);
        _antag.SendBriefing(ent, Loc.GetString("cosmiccult-role-deconverted-briefing"), Color.FromHex("#cae8e8"), null);

        if (!_mind.TryGetMind(ent, out var mindId, out _)
            || !TryComp<MindComponent>(mindId, out var mindComp))
            return;

        _mind.ClearObjectives((mindId, mindComp));
        _role.MindRemoveRole<CosmicCultRoleComponent>(mindId);

        if (_player.TryGetSessionById(mindComp.UserId, out var session))
            _euiMan.OpenEui(new CosmicDeconvertedEui(), session);

        _eye.SetVisibilityMask(ent, 1);
        cosmicGamerule.TotalCult--;
        cosmicGamerule.Cultists.Remove(ent);

        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnComponentShutdown(Entity<CosmicLesserCultistComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _actions.RemoveAction(ent.Comp.DamageTransferActionEntity);

        if (TryComp<IntrinsicRadioTransmitterComponent>(ent, out var transmitter))
            transmitter.Channels.Remove("CosmicRadio");

        if (TryComp<ActiveRadioComponent>(ent, out var radio))
            radio.Channels.Remove("CosmicRadio");

        RemComp<PressureImmunityComponent>(ent);
        RemComp<SpecialLowTempImmunityComponent>(ent);
        RemComp<CosmicNonRespiratingComponent>(ent);
        RemComp<CosmicSubtleMarkComponent>(ent);

        if (!ent.Comp.WasWeakToHoly)
        {
            RemComp<WeakToHolyComponent>(ent);
            var ev = new UnholyStatusChangedEvent(ent, ent, false);
            RaiseLocalEvent(ent, ref ev);
        }

        _antag.SendBriefing(ent, Loc.GetString("cosmiccult-role-deconverted-fluff"), Color.FromHex("#4cabb3"), _deconvertSound);
        _antag.SendBriefing(ent, Loc.GetString("cosmiccult-role-deconverted-briefing"), Color.FromHex("#cae8e8"), null);
        _eye.SetVisibilityMask(ent, 1);

        if (!_mind.TryGetMind(ent, out var mindId, out _)
            || !TryComp<MindComponent>(mindId, out var mindComp))
            return;

        _mind.ClearObjectives((mindId, mindComp));
        _role.MindRemoveRole<CosmicCultRoleComponent>(mindId);

        if (_player.TryGetSessionById(mindComp.UserId, out var session))
            _euiMan.OpenEui(new CosmicDeconvertedEui(), session);
    }
    #endregion
}

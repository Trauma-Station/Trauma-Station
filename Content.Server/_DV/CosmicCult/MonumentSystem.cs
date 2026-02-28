using Content.Server.Actions;
using Content.Server.AlertLevel;
using Content.Server.Audio;
using Content.Server.Objectives.Components;
using Content.Server.Pinpointer;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Server.Chat.Systems;
using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared._DV.CosmicCult.Components.Examine;
using Content.Shared.Audio;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Parallax;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._DV.CosmicCult;

public sealed class MonumentSystem : SharedMonumentSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly CosmicCultRuleSystem _cultRule = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedCosmicCultSystem _cult = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly AlertLevelSystem _alert = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly RoundEndSystem _evac = default!;
    private HashSet<Entity<HumanoidProfileComponent>> _targets = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MonumentComponent, ComponentStartup>(OnStartMonument);
        SubscribeLocalEvent<MonumentComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<MonumentComponent, StartFinaleDoAfterEvent>(OnFinaleStartDoAfter);
        SubscribeLocalEvent<MonumentComponent, CancelFinaleDoAfterEvent>(OnFinaleCancelDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MonumentComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.MusicTimer is { } musicTimer
                && _timing.CurTime >= musicTimer)
            {
                _sound.StopStationEventMusic(uid, StationEventMusicType.CosmicCult);
                _sound.DispatchStationEventMusic(uid, comp.BufferMusicLoop, StationEventMusicType.CosmicCult, comp.BufferMusicLoop.Params);

                comp.MusicTimer = null;
            }

            if (comp.BufferTimer is { } bufferTimer
                && _timing.CurTime >= bufferTimer)
            {
                _sound.StopStationEventMusic(uid, StationEventMusicType.CosmicCult);
                _sound.DispatchStationEventMusic(uid, comp.FinaleMusic, StationEventMusicType.CosmicCult, comp.FinaleMusic.Params);
                _chatSystem.DispatchStationAnnouncement(
                    uid,
                    Loc.GetString("cosmiccult-announce-finale-warning"),
                    null,
                    false,
                    null,
                    Color.FromHex("#cae8e8"));

                comp.Stage++;
                UpdateMonumentAppearance((uid, comp));

                comp.MusicTimer = null;
                comp.BufferTimer = null;
                comp.FinaleTimer = _timing.CurTime + comp.FinaleTime;
            }

            if (comp.FinaleTimer is { } finaleTimer
                && _timing.CurTime >= finaleTimer)
            {
                Spawn(comp.CosmicGod, Transform(uid).Coordinates);

                var victoryQuery = EntityQueryEnumerator<CosmicVictoryConditionComponent>();
                while (victoryQuery.MoveNext(out _, out var victoryComp))
                    victoryComp.Victory = true;

                comp.FinaleTimer = null;
            }
        }
    }

    private void OnStartMonument(Entity<MonumentComponent> ent, ref ComponentStartup args)
    {
        UpdateMonumentAppearance(ent); // Why don't just spawn it with stage 3 visuals? Because the spawn animation is for stage 2, and I'm NOT redoing that.
        ent.Comp.CanActivate = true;
    }

    private void OnInteract(Entity<MonumentComponent> ent, ref InteractHandEvent args)
    {
        if (!HasComp<HumanoidProfileComponent>(args.User) // Humanoids only!
        || HasComp<MonumentTransformingComponent>(ent) // Let the animation play ffs it's not even 3 seconds long
        || args.Handled)
            return;

        if (!_cult.EntityIsCultist(args.User))
        {
            var doargs = new DoAfterArgs(EntityManager,
                args.User,
                ent.Comp.InteractionTime,
                new CancelFinaleDoAfterEvent(),
                ent,
                ent)
            {
                DistanceThreshold = 1f,
                Hidden = false,
                BreakOnHandChange = true,
                BreakOnDamage = true,
                BreakOnMove = true
            };
            _popup.PopupEntity(Loc.GetString("cosmiccult-finale-cancel-begin"), args.User, args.User);
            _doAfter.TryStartDoAfter(doargs);
            args.Handled = true;
        }
        else if (_cult.EntityIsCultist(args.User) && !ent.Comp.Active && ent.Comp.CanActivate)
        {
            var doargs = new DoAfterArgs(EntityManager,
                args.User,
                ent.Comp.InteractionTime,
                new StartFinaleDoAfterEvent(),
                ent,
                ent)
            {
                DistanceThreshold = 1f,
                Hidden = false,
                BreakOnHandChange = true,
                BreakOnDamage = true,
                BreakOnMove = true
            };
            _popup.PopupEntity(Loc.GetString("cosmiccult-finale-beckon-begin"), args.User, args.User);
            _doAfter.TryStartDoAfter(doargs);
            args.Handled = true;
        }
        else if (_cult.EntityIsCultist(args.User) && ent.Comp.Active && ent.Comp.BufferTimer != null)
        {
            _targets.Clear();
            _lookup.GetEntitiesInRange(Transform(ent).Coordinates, range: 2f, _targets);
            _targets.RemoveWhere(target => !_mobState.IsCritical(target) || HasComp<CosmicSacrificedComponent>(target));
            if (_targets.Count == 0)
            {
                _popup.PopupEntity(Loc.GetString("cosmiccult-finale-nobodyes"), ent);
                return;
            }
            _popup.PopupEntity(Loc.GetString("cosmiccult-finale-speedup"), ent);
            _audio.PlayPvs(ent.Comp.SacrificeSfx, ent); // Do it once to avoid sound spam for multiple targets.
            foreach (var target in _targets)
            {
                Spawn(ent.Comp.SacrificeVfx, Transform(target).Coordinates);
                ent.Comp.BufferTimer -= ent.Comp.BufferSacrificeSpeedup;
                EnsureComp<CosmicBlankComponent>(target);
                EnsureComp<CosmicSacrificedComponent>(target, out var sacComp);
                sacComp.WasNonRespirating = HasComp<CosmicNonRespiratingComponent>(target);
                EnsureComp<CosmicNonRespiratingComponent>(target).EnableWhenCritical = true;
                if (!_mind.TryGetMind(target, out var mind, out _)) continue;
                var vessel = Spawn(ent.Comp.SacrificeVessel, Transform(target).Coordinates);
                sacComp.AstralForm = vessel;
                _mind.TransferTo(mind, vessel);
            }
        }
    }

    private void OnFinaleStartDoAfter(Entity<MonumentComponent> ent, ref StartFinaleDoAfterEvent args)
    {
        if (args.Args.Target == null
        || args.Cancelled
        || args.Handled
        || !ent.Comp.CanActivate)
            return;

        if (_cultRule.AssociatedGamerule(ent) is not { } cult) return;

        _sound.DispatchStationEventMusic(ent, ent.Comp.BufferMusic, StationEventMusicType.CosmicCult, ent.Comp.BufferMusic.Params);
        ent.Comp.MusicTimer = _timing.CurTime + _audio.GetAudioLength(_audio.ResolveSound(ent.Comp.BufferMusic)) - TimeSpan.FromSeconds(5); // -5 seconds to compensate for lag and shit, it loops fine either way
        ent.Comp.BufferTimer = _timing.CurTime + ent.Comp.BufferTime;

        var mapData = _map.GetMap(_transform.GetMapId(Transform(ent).Coordinates));

        EnsureComp<ParallaxComponent>(mapData, out var parallax);
        parallax.Parallax = "CosmicFinaleParallax";
        Dirty(mapData, parallax);

        EnsureComp<MapLightComponent>(mapData, out var mapLight); // This thing just doesn't work for some reason and I can't figure out why
        mapLight.AmbientLightColor = Color.FromHex("#210746");
        Dirty(mapData, mapLight);

        cult.Comp.MonumentInGame = ent;

        var stationUid = _station.GetOwningStation(ent);
        if (stationUid != null)
            _alert.SetLevel(stationUid.Value, "octarine", true, true, true, true);

        var indicatedLocation = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((ent, Transform(ent))));
        _chatSystem.DispatchStationAnnouncement(ent,
            Loc.GetString("cosmiccult-finale-location", ("location", indicatedLocation)),
            null,
            false,
            null,
            Color.FromHex("#cae8e8"));

        foreach (var cultist in cult.Comp.Cultists)
        {
            RemComp<CosmicSubtleMarkComponent>(cultist);
            EnsureComp<CosmicStarMarkComponent>(cultist);
        }
        ent.Comp.Stage++;
        UpdateMonumentAppearance(ent);

        _evac.CancelRoundEndCountdown(args.User, args.Used, forceRecall: true);

        ent.Comp.CanActivate = false;
        ent.Comp.Active = true;

        Dirty(ent, ent.Comp);
    }

    private void OnFinaleCancelDoAfter(Entity<MonumentComponent> ent, ref CancelFinaleDoAfterEvent args)
    {
        if (_cultRule.AssociatedGamerule(ent) is not { } cult
        || args.Cancelled
        || args.Handled)
            return;

        cult.Comp.MonumentInGame = null;

        _sound.StopStationEventMusic(ent, StationEventMusicType.CosmicCult);

        var stationUid = _station.GetOwningStation(ent);
        if (stationUid != null)
            _alert.SetLevel(stationUid.Value, "blue", true, true, true); // Blue makes more sense than green IMO.

        foreach (var cultist in cult.Comp.Cultists)
        {
            if (!TryComp<CosmicCultComponent>(cultist, out var cultComp)) continue;
            cultComp.MonumentActionEntity = _actions.AddAction(cultist, cultComp.MonumentAction);
        }

        var query = EntityQueryEnumerator<CosmicSacrificedComponent>(); // All the sacrificed people go back to normal. No healing though.
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mind.TryGetMind(comp.AstralForm, out var mind, out _))
                _mind.TransferTo(mind, uid);
            RemComp<CosmicBlankComponent>(uid);
            if (TryComp<CosmicNonRespiratingComponent>(uid, out var respComp))
            {
                if (comp.WasNonRespirating)
                {
                    respComp.EnableWhenCritical = false;
                    respComp.EnableWhenAlive = true;
                }
                else
                    RemComp<CosmicNonRespiratingComponent>(uid);
            }
            RemComp<CosmicSacrificedComponent>(uid);
            QueueDel(comp.AstralForm);
        }

        Spawn(ent.Comp.DespawnVfx, Transform(ent).Coordinates);
        QueueDel(ent);
    }

    /// <summary>
    /// Updates the monument's visuals to the next stage. Runs on hardcoded bullshit and magic numbers, because nothing is nice with animations in this engine.
    /// </summary>
    public void UpdateMonumentAppearance(Entity<MonumentComponent> ent)
    {
        if (ent.Comp.Stage >= 5) return;
        _appearance.SetData(ent, MonumentVisuals.Monument, Math.Clamp(ent.Comp.Stage, 1, 2));
        _appearance.SetData(ent, MonumentVisuals.FinaleReached, Math.Clamp(ent.Comp.Stage - 1, 0, 3));

        if (ent.Comp.Stage == 2)
        {
            EnsureComp<MonumentTransformingComponent>(ent, out var comp);
            _appearance.SetData(ent, MonumentVisuals.Transforming, true);
            comp.EndTime = _timing.CurTime + ent.Comp.TransformTime;
        }
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Actions;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Cloning;
using Content.Server.Flash;
using Content.Server.Hands.Systems;
using Content.Server.Polymorph.Systems;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Localizations;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Store.Components;
using Content.Shared.Stunnable;
using Content.Shared.Weather;
using Content.Trauma.Common.CollectiveMind;
using Content.Trauma.Server.Heretic.Systems.PathSpecific;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Systems.Abilities;
using Content.Trauma.Shared.Wizard.SanguineStrike;
using Robust.Server.Containers;
using Robust.Server.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Trauma.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem : SharedHereticAbilitySystem
{
    #region Dependencies

    [Dependency] private StoreSystem _store = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private PolymorphSystem _poly = default!;
    [Dependency] private MobStateSystem _mobstate = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private SharedStaminaSystem _stam = default!;
    [Dependency] private SharedAudioSystem _aud = default!;
    [Dependency] private FlashSystem _flash = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private IMapManager _mapMan = default!;
    [Dependency] private BloodstreamSystem _blood = default!;
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;
    [Dependency] private PvsOverrideSystem _pvs = default!;
    [Dependency] private CloningSystem _cloning = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SharedSanguineStrikeSystem _lifesteal = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private BladeArenaSystem _arena = default!;

    #endregion

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EventHereticOpenStore>(OnStore);

        SubscribeLocalEvent<EventHereticLivingHeart>(OnLivingHeart);
        SubscribeLocalEvent<EventHereticLivingHeartActivate>(OnLivingHeartActivate);

        SubscribeLocalEvent<EventHereticMansusLink>(OnMansusLink);
        SubscribeLocalEvent<HereticMansusLinkDoAfter>(OnMansusLinkDoafter);
    }

    private void OnStore(EventHereticOpenStore args)
    {
        if (!TryUseAbility(args))
            return;

        if (!Heretic.TryGetHereticComponent(args.Performer, out _, out var ent))
            return;

        if (!TryComp<StoreComponent>(ent, out var store))
            return;

        _store.ToggleUi(args.Performer, ent, store);
    }

    private void OnLivingHeart(EventHereticLivingHeart args)
    {
        if (!TryUseAbility(args))
            return;

        if (!Heretic.TryGetHereticComponent(args.Performer, out var heretic, out var mind))
            return;

        if (!TryComp<UserInterfaceComponent>(mind, out var uic))
            return;

        var uid = args.Performer;

        if (heretic.SacrificeTargets.Count == 0)
        {
            Popup.PopupEntity(Loc.GetString("heretic-livingheart-notargets"), uid, uid);
            return;
        }

        _ui.OpenUi((mind, uic), HereticLivingHeartKey.Key, uid);
    }

    private void OnLivingHeartActivate(EventHereticLivingHeartActivate args)
    {
        string loc;

        if (!Heretic.TryGetHereticComponent(args.Actor, out var heretic, out _))
            return;

        if (heretic.SacrificeTargets.All(x => x.Entity != args.Target))
            return;

        if (!TryGetEntity(args.Target, out var target))
            return;

        if (!TryComp<MobStateComponent>(target, out var mobstate))
            return;

        var uid = args.Actor;

        var state = mobstate.CurrentState;
        var locstate = state.ToString().ToLower();

        var ourMapCoords = _transform.GetMapCoordinates(uid);
        var targetMapCoords = _transform.GetMapCoordinates(target.Value);

        if (_map.IsPaused(targetMapCoords.MapId))
            loc = Loc.GetString("heretic-livingheart-unknown");
        else if (targetMapCoords.MapId != ourMapCoords.MapId)
            loc = Loc.GetString("heretic-livingheart-faraway", ("state", locstate));
        else
        {
            var targetStation = _station.GetOwningStation(target);
            var ownStation = _station.GetOwningStation(uid);

            var isOnStation = targetStation != null && targetStation == ownStation;

            var ang = Angle.Zero;
            if (_mapMan.TryFindGridAt(_transform.GetMapCoordinates(Transform(uid)), out var grid, out _))
                ang = Transform(grid).LocalRotation;

            var vector = targetMapCoords.Position - ourMapCoords.Position;
            var direction = (vector.ToWorldAngle() - ang).GetDir();

            var locdir = ContentLocalizationManager.FormatDirection(direction).ToLower();

            loc = Loc.GetString(isOnStation ? "heretic-livingheart-onstation" : "heretic-livingheart-offstation",
                ("state", locstate),
                ("direction", locdir));
        }

        Popup.PopupEntity(loc, uid, uid, PopupType.Medium);
        _aud.PlayPvs(new SoundPathSpecifier("/Audio/_Goobstation/Heretic/heartbeat.ogg"),
            uid,
            AudioParams.Default.WithVolume(-3f));
    }

    private void OnMansusLink(EventHereticMansusLink args)
    {
        if (!TryUseAbility(args))
            return;

        var ent = args.Performer;
        var target = args.Target;
        if (!HasComp<MindContainerComponent>(target))
        {
            Popup.PopupEntity(Loc.GetString("heretic-manselink-fail-nomind"), ent, ent);
            return;
        }

        if (TryComp<CollectiveMindComponent>(target, out var mind) && mind.Channels.Contains(MansusLinkMind))
        {
            Popup.PopupEntity(Loc.GetString("heretic-manselink-fail-exists"), ent, ent);
            return;
        }

        var dargs = new DoAfterArgs(EntityManager,
            ent,
            5f,
            new HereticMansusLinkDoAfter(),
            eventTarget: ent,
            target: target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            MultiplyDelay = false
        };
        Popup.PopupEntity(Loc.GetString("heretic-manselink-start"), ent, ent);
        Popup.PopupEntity(Loc.GetString("heretic-manselink-start-target"), target, target, PopupType.MediumCaution);
        DoAfter.TryStartDoAfter(dargs);
    }

    private void OnMansusLinkDoafter(HereticMansusLinkDoAfter args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        EnsureComp<CollectiveMindComponent>(target).Channels.Add(MansusLinkMind);

        _flash.Flash(target,
            null,
            null,
            TimeSpan.FromSeconds(2f),
            0f,
            false,
            true,
            stunDuration: TimeSpan.FromSeconds(1f));
    }
}

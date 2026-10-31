// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Actions;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Cloning;
using Content.Server.Hands.Systems;
using Content.Server.Polymorph.Systems;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Localizations;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Station.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Weather;
using Content.Trauma.Server.Heretic.Systems;
using Content.Trauma.Server.Heretic.Systems.PathSpecific;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Systems.Abilities;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem : SharedHereticAbilitySystem
{
    #region Dependencies

    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private PolymorphSystem _poly = default!;
    [Dependency] private MobStateSystem _mobstate = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private SharedStaminaSystem _stam = default!;
    [Dependency] private SharedAudioSystem _aud = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;
    [Dependency] private CloningSystem _cloning = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] private BladeArenaSystem _arena = default!;
    [Dependency] private HereticSystem _heretic = default!;

    #endregion

    [SubscribeLocalEvent]
    private void OnLivingHeart(EventHereticLivingHeart args)
    {
        if (!TryUseAbility(args))
            return;

        if (!Heretic.TryGetHereticComponent(args.Performer, out var heretic, out var mind))
            return;

        if (!TryComp<UserInterfaceComponent>(mind, out var uic))
            return;

        _heretic.UpdateHereticTargets((mind, heretic));

        var uid = args.Performer;

        if (heretic.SacrificeTargets.Count == 0)
        {
            Popup.PopupEntity(Loc.GetString("heretic-livingheart-notargets"), uid, uid);
            return;
        }

        _ui.OpenUi((mind, uic), HereticLivingHeartKey.Key, uid);
    }

    [SubscribeLocalEvent]
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
            if (_map.TryFindGridAt(_transform.GetMapCoordinates(Transform(uid)), out var grid, out _))
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
}

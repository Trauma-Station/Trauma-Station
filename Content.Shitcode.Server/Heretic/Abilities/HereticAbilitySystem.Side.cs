// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <aviu00@protonmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Polymorph.Components;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Actions;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Ghost;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Mobs.Components;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    protected override void SubscribeSide()
    {
        base.SubscribeSide();

        SubscribeLocalEvent<EventHereticCleave>(OnCleave);
        SubscribeLocalEvent<EventHereticSpacePhase>(OnSpacePhase);
        SubscribeLocalEvent<EventMirrorJaunt>(OnMirrorJaunt);

        SubscribeLocalEvent<HereticComponent, HereticGraspUpgradeEvent>(OnGraspUpgrade);
        SubscribeLocalEvent<HereticComponent, HereticRemoveActionEvent>(OnRemoveAction);
    }

    private void OnRemoveAction(Entity<HereticComponent> ent, ref HereticRemoveActionEvent args)
    {
        if (!_actions.TryGetActionById(ent.Owner, args.Action, out var act))
            return;

        _actionContainer.RemoveAction(act.Value.AsNullable());
    }

    private void OnGraspUpgrade(Entity<HereticComponent> ent, ref HereticGraspUpgradeEvent args)
    {
        if (!_actions.TryGetActionById(ent.Owner, args.GraspAction, out var grasp))
            return;

        var upgrade = EnsureComp<MansusGraspUpgradeComponent>(grasp.Value);
        foreach (var (key, value) in args.AddedComponents)
        {
            upgrade.AddedComponents[key] = value;
        }
    }

    private void OnMirrorJaunt(EventMirrorJaunt args)
    {
        var uid = args.Performer;

        if (Lookup.GetEntitiesInRange<ReflectiveSurfaceComponent>(Transform(uid).Coordinates, args.LookupRange).Count == 0)
        {
            Popup.PopupEntity(Loc.GetString("heretic-ability-fail-mirror-jaunt-no-mirrors"), uid, uid);
            return;
        }

        TryPerformJaunt(uid, args, args.Polymorph);
    }

    private void OnSpacePhase(EventHereticSpacePhase args)
    {
        var uid = args.Performer;

        var xform = Transform(uid);
        var mapCoords = _transform.GetMapCoordinates(uid, xform);

        if (_mapMan.TryFindGridAt(mapCoords, out var gridUid, out var mapGrid) &&
            _map.TryGetTileRef(gridUid, mapGrid, xform.Coordinates, out var tile) &&
            (!_weather.CanWeatherAffect((gridUid, mapGrid), tile) ||
             _atmos.GetTileMixture(gridUid, xform.MapUid, tile.GridIndices)?.Pressure is
                 > Atmospherics.WarningLowPressure))
        {
            Popup.PopupEntity(Loc.GetString("heretic-ability-fail-space-phase-not-space"), uid, uid);
            return;
        }

        if (!TryPerformJaunt(uid, args, args.Polymorph))
            return;

        Spawn(args.Effect, mapCoords);
    }

    private bool TryPerformJaunt(EntityUid uid,
        BaseActionEvent args,
        ProtoId<PolymorphPrototype> polymorph)
    {
        if (TryComp(uid, out PolymorphedEntityComponent? morphed) && HasComp<SpectralComponent>(uid))
            _poly.Revert((uid, morphed));
        else if (TryUseAbility(args))
            _poly.PolymorphEntity(uid, polymorph);
        else
            return false;
        return true;
    }

    private void OnCleave(EventHereticCleave args)
    {
        if (!TryUseAbility(args))
            return;

        args.Handled = true;

        if (!args.Target.IsValid(EntityManager))
            return;

        Spawn(args.Effect, args.Target);

        var bloodQuery = GetEntityQuery<BloodstreamComponent>();

        var hasTargets = false;

        var targets = Lookup.GetEntitiesInRange<MobStateComponent>(args.Target, args.Range, LookupFlags.Dynamic);
        foreach (var (target, _) in targets)
        {
            if (target == args.Performer)
                continue;

            hasTargets = true;

            _dmg.TryChangeDamage(target, args.Damage, true, origin: args.Performer);

            if (!bloodQuery.TryComp(target, out var blood))
                continue;

            _blood.TryModifyBloodLevel((target, blood), args.BloodModifyAmount);
            _blood.TryModifyBleedAmount((target, blood), blood.MaxBleedAmount);
        }

        if (hasTargets)
            _aud.PlayPvs(args.Sound, args.Target);
    }
}

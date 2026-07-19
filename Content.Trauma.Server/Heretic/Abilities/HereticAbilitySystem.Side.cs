// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Wounds;
using Content.Server.Polymorph.Components;
using Content.Shared.Actions;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Ghost;
using Content.Shared.Polymorph;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    [Dependency] private EntityQuery<BloodstreamComponent> _bloodQuery = default!;

    private readonly List<EntityUid> _bloodStealTargets = new();
    private readonly HashSet<Entity<ReflectiveSurfaceComponent>> _mirrors = new();

    [SubscribeLocalEvent]
    private void OnAddMindComponents(Entity<HereticComponent> ent, ref HereticAddMindComponentsEvent args)
    {
        EntityManager.AddComponents(ent, args.AddedComponents);
    }

    [SubscribeLocalEvent]
    private void OnRemoveAction(Entity<HereticComponent> ent, ref HereticRemoveActionEvent args)
    {
        if (!_actions.TryGetActionById(ent.Owner, args.Action, out var act))
            return;

        _actionContainer.RemoveAction(act.Value.AsNullable());
    }

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
    private void OnMirrorJaunt(EventMirrorJaunt args)
    {
        var uid = args.Performer;
        var coords = Transform(uid).Coordinates;
        _mirrors.Clear();
        Lookup.GetEntitiesInRange(coords, args.LookupRange, _mirrors);
        if (_mirrors.Count == 0)
        {
            Popup.PopupEntity(Loc.GetString("heretic-ability-fail-mirror-jaunt-no-mirrors"), uid, uid);
            return;
        }

        TryPerformJaunt(uid, args, args.Polymorph);
    }

    [SubscribeLocalEvent]
    private void OnSpacePhase(EventHereticSpacePhase args)
    {
        var uid = args.Performer;

        var xform = Transform(uid);
        var mapCoords = _transform.GetMapCoordinates(uid, xform);

        if (_map.TryFindGridAt(mapCoords, out var gridUid, out var mapGrid) &&
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
        var ourId = Prototype(args.Action)?.ID;
        if (TryComp(uid, out PolymorphedEntityComponent? morphed) && HasComp<SpectralComponent>(uid))
        {
            if (_poly.Revert((uid, morphed)) is not { } reverted)
                return false;

            if (ourId is not { } id1)
                return true;

            // Reset jaunt cooldown
            foreach (var action in _actions.GetActions(reverted))
            {
                if (Prototype(action)?.ID is { } id2 && id2 == id1)
                    _actions.StartUseDelay(action.AsNullable());
            }
        }
        else if (!TryUseAbility(args) || _poly.PolymorphEntity(uid, polymorph) == null)
            return false;

        return true;
    }

    [SubscribeLocalEvent]
    private void OnCleave(EventHereticCleave args)
    {
        if (!TryUseAbility(args))
            return;

        args.Handled = true;

        if (!args.Target.IsValid())
            return;

        var coords = Transform(args.Target).Coordinates;
        Spawn(args.Effect, coords);

        var hasTargets = false;

        TryComp(args.Performer, out DamageableComponent? damageable);

        _bloodStealTargets.Clear();
        foreach (var (target, _) in GetNearbyPeople(args.Performer, args.Range, null, coords))
        {
            if (target == args.Performer)
                continue;

            hasTargets = true;

            var dmg = _dmg.ChangeDamage(target, args.Damage, true, origin: args.Performer);
            if (damageable != null)
                _lifesteal.LifeSteal((args.Performer, damageable), dmg.GetTotal());

            if (!_bloodQuery.TryComp(target, out var blood))
                continue;

            _blood.TryModifyBleedAmount((target, blood), blood.MaxBleedAmount);
            _bloodStealTargets.Add(target);
        }

        if (!hasTargets)
            return;

        foreach (var (_, woundable) in _body.GetOrgans<WoundableComponent>(args.Performer))
        {
            _container.EmptyContainer(woundable.Wounds);
        }

        _lifesteal.BloodSteal(args.Performer, _bloodStealTargets, args.BloodModifyAmount, null);

        _aud.PlayPvs(args.Sound, args.Target);
    }
}

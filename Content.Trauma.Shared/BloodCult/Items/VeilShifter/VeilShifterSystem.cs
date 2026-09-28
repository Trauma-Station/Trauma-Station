// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Trauma.Shared.Teleportation;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.BloodCult.Items.VeilShifter;

public sealed partial class VeilShifterSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TeleportSystem _teleport = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<VeilShifterComponent> veil, ref UseInHandEvent args)
    {
        // TODO: check if tg lets non cultists use it

        if (!TryComp<LimitedChargesComponent>(veil, out var charges) ||
            !_charges.HasCharges((veil.Owner, charges), 1))
            return;

        if (!Teleport(veil, args.User))
            return;

        _charges.TryUseCharge((veil.Owner));
        args.Handled = true;
    }

    private bool Teleport(Entity<VeilShifterComponent> veil, EntityUid user)
    {
        var xform = Transform(user);

        EntityCoordinates coords = default;
        var direction = xform.LocalRotation.GetDir().ToVec();
        var offset = xform.LocalRotation.ToWorldVec().Normalized();

        var foundPos = false;

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(veil));
        // tries20 my beloved
        for (var i = 0; i < veil.Comp.Attempts; i++)
        {
            var distance = rand.Next(veil.Comp.TeleportDistanceMin, veil.Comp.TeleportDistanceMax);
            coords = xform.Coordinates.Offset(offset + direction * distance).SnapToGrid();

            if (_transform.GetGrid(coords) is {} gridUid &&
                _gridQuery.TryComp(gridUid, out var grid) &&
                _map.TryGetTileRef(gridUid, grid, coords, out var tile) &&
                !_turf.IsTileBlocked(tile, CollisionGroup.MobMask))
            {
                foundPos = true;
                break;
            }
        }

        if (!foundPos)
        {
            _popup.PopupEntity(Loc.GetString("veil-shifter-cant-teleport"), veil, user);
            return false;
        }

        var oldCoords = xform.Coordinates;
        _teleport.Teleport(user, coords, veil.Comp.TeleportInSound, veil.Comp.TeleportOutSound, user);
        PredictedSpawnAtPosition(veil.Comp.TeleportInEffect, coords);
        PredictedSpawnAtPosition(veil.Comp.TeleportOutEffect, oldCoords);
        return true;
    }
}

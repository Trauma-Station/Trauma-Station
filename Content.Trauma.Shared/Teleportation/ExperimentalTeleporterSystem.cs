// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Coordinates.Helpers;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using Content.Shared.Timing.Systems;
using Content.Shared.Wall;
using Content.Trauma.Shared.Standing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Teleportation;

public sealed partial class ExperimentalTeleporterSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private UseDelaySystem _delay = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private TelefragSystem _telefrag = default!;
    [Dependency] private TeleportSystem _teleport = default!;
    [Dependency] private TurfSystem _turf = default!;

    [Dependency] private EntityQuery<WallComponent> _wallQuery = default!;

    public static readonly ProtoId<TagPrototype> DirectionalTag = "Directional";

    [SubscribeLocalEvent]
    private void OnUse(Entity<ExperimentalTeleporterComponent> ent, ref UseInHandEvent args)
    {
        var user = args.User;
        if (_delay.IsDelayed(ent.Owner)
            || (_container.IsEntityInContainer(user)
            && !_container.TryRemoveFromContainer(user)))
            return;

        var xform = Transform(user);
        var oldCoords = xform.Coordinates.SnapToGrid(EntityManager);
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var range = rand.Next(ent.Comp.MinTeleportRange, ent.Comp.MaxTeleportRange);
        var offset = xform.LocalRotation.ToWorldVec().Normalized();
        var direction = xform.LocalRotation.GetDir().ToVec();
        var newOffset = offset + direction * range;

        var coords = xform.Coordinates.Offset(newOffset).SnapToGrid(EntityManager);

        // Delay is applied even if the teleport failed
        _delay.TryResetDelay(ent.Owner);

        if (TryCheckWall(coords)
            || !_teleport.Teleport(user, coords, ent.Comp.TeleportSound, user))
        {
            _popup.PopupPredicted("Teleportation failed!", ent, user, PopupType.MediumCaution);
            _audio.PlayPredicted(ent.Comp.FailSound, oldCoords, user);
            return;
        }

        _telefrag.DoTelefrag(user, coords, TimeSpan.Zero);
        PredictedSpawnAtPosition(ent.Comp.TeleportInEffect, coords);
        PredictedSpawnAtPosition(ent.Comp.TeleportOutEffect, oldCoords);
    }

    /// <summary>
    /// Returns true if there is a wall at the given coordinates
    /// </summary>
    private bool TryCheckWall(EntityCoordinates coords)
    {
        if (!_turf.TryGetTileRef(coords, out var tile)
            || !TryComp<MapGridComponent>(tile.Value.GridUid, out var mapGridComponent))
            return false;

        var anchoredEntities = _map.GetAnchoredEntities(tile.Value.GridUid, mapGridComponent, coords);
        foreach (var x in anchoredEntities)
        {
            if (_wallQuery.HasComp(x) && !_tag.HasTag(x, DirectionalTag))
                return true;
        }

        return false;
    }
}

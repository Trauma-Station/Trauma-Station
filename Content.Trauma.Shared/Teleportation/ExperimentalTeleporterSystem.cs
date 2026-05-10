// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Charges.Systems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Gibbing;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using Content.Trauma.Shared.Standing;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Teleportation;

public sealed class ExperimentalTeleporterSystem : EntitySystem
{
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly TelefragSystem _telefrag = default!;
    [Dependency] private readonly TeleportSystem _teleport = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public static readonly ProtoId<TagPrototype> WallTag = "Wall";

    private List<EntityUid> _gibQueue = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExperimentalTeleporterComponent, UseInHandEvent>(OnUse);
    }

    public override void Update(float frameTime)
    {
        base.Initialize();

        foreach (var uid in _gibQueue)
        {
            _gibbing.Gib(uid);
        }
        _gibQueue.Clear();
    }

    private void OnUse(Entity<ExperimentalTeleporterComponent> ent, ref UseInHandEvent args)
    {
        var user = args.User;
        if (_charges.IsEmpty(ent.Owner)
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

        Teleport(user, ent, coords, oldCoords);

        if (!TryCheckWall(coords)
            || EmergencyTeleportation((user, xform), ent, rand, oldCoords, newOffset))
            return;

        // has to be defered because of interaction system's expectations that the user isn't being deleted
        _popup.PopupClient("Teleporter malfunction", ent, user, PopupType.LargeCaution);
        _gibQueue.Add(user);
    }

    private bool EmergencyTeleportation(Entity<TransformComponent> user, Entity<ExperimentalTeleporterComponent> ent, System.Random rand, EntityCoordinates oldCoords, Vector2 offset)
    {
        if (_charges.IsEmpty(ent.Owner))
            return false;

        _popup.PopupClient("Emergency teleport saved your life!", ent, user, PopupType.LargeCaution);
        var newOffset = offset + RandomEmergencyOffset(ent, rand, offset);
        var coords = user.Comp.Coordinates.Offset(newOffset).SnapToGrid(EntityManager);

        Teleport(user, ent, coords, oldCoords);

        return !TryCheckWall(coords);
    }

    private void Teleport(EntityUid user, Entity<ExperimentalTeleporterComponent> ent, EntityCoordinates coords, EntityCoordinates oldCoords)
    {
        var sound = ent.Comp.TeleportSound;
        if (!_teleport.Teleport(user, coords, sound, user))
            return;

        _telefrag.DoTelefrag(user, coords, TimeSpan.Zero);
        SpawnEffects(ent.Comp, coords, oldCoords);
        _charges.TryUseCharge(ent.Owner);
    }

    private void SpawnEffects(ExperimentalTeleporterComponent comp, EntityCoordinates coords, EntityCoordinates oldCoords)
    {
        PredictedSpawnAtPosition(comp.TeleportInEffect, coords);
        PredictedSpawnAtPosition(comp.TeleportOutEffect, oldCoords);
    }

    private bool TryCheckWall(EntityCoordinates coords)
    {
        if (!_turf.TryGetTileRef(coords, out var tile)
            || !TryComp<MapGridComponent>(tile.Value.GridUid, out var mapGridComponent))
            return false;

        var anchoredEntities = _map.GetAnchoredEntities(tile.Value.GridUid, mapGridComponent, coords);
        foreach (var x in anchoredEntities)
        {
            if (_tag.HasTag(x, WallTag))
                return true;
        }

        return false;
    }

    private Vector2 RandomEmergencyOffset(Entity<ExperimentalTeleporterComponent> ent, System.Random rand, Vector2 offset)
    {
        if (ent.Comp.RandomRotations.Count == 0)
            return Vector2.Zero;

        var length = ent.Comp.EmergencyLength;
        var rotation = rand.Pick(ent.Comp.RandomRotations);
        return rotation.RotateVec(offset.Normalized() * length);
    }
}

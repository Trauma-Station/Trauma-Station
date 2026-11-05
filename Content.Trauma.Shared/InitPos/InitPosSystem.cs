// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Trauma.Shared.InitPos;

/// <summary>
/// Stores ent's parent location on ent's init, map and grid included.
/// </summary>
public sealed partial class InitPosSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _xform = default!;

    [SubscribeLocalEvent]
    private void Startup(Entity<InitPosComponent> ent, ref ComponentStartup args)
    {
        var xform = Transform(ent);

        if (ent.Comp.Coordinates.Position == Vector2.Zero || ent.Comp.Coordinates.Position == Vector2Helpers.NaN)
        {
            ent.Comp.Coordinates = xform.Coordinates;
            ent.Comp.Map = xform.MapID;
        }

        Dirty(ent.Owner, ent.Comp);
    }

    public void SpawnWithParentPos(string? protoName, Entity<TransformComponent> parent)
    {
        var uid = PredictedSpawnAtPosition(protoName, parent.Comp.Coordinates);

        var Comp = EnsureComp<InitPosComponent>(uid);

        Comp.Coordinates = parent.Comp.Coordinates;
        Comp.Map = parent.Comp.MapID;
        Dirty(uid, Comp);
    }
}

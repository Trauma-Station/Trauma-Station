// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Fluids;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.Fluids;

public sealed partial class PuddleSpawnerSystem : EntitySystem
{
    [Dependency] private SharedPuddleSystem _puddle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PuddleSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<PuddleSpawnerComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent);
        if (xform.MapID == MapId.Nullspace)
            return; // spawn menu etc dont care

        _puddle.TrySpillAt(xform.Coordinates, ent.Comp.Solution, out _);
        PredictedQueueDel(ent);
    }
}

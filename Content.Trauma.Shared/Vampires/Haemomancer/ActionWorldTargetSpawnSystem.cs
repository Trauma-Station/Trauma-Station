// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Trauma.Shared.Vampires.Haemomancer;

public sealed partial class ActionWorldTargetSpawnSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionWorldTargetSpawnComponent, WorldTargetSpawnActionEvent>(OnPerform);
    }

    private void OnPerform(Entity<ActionWorldTargetSpawnComponent> ent, ref WorldTargetSpawnActionEvent args)
    {
        if (_net.IsClient && !ent.Comp.Predicted)
            return;

        // Only target tiles
        if (args.Entity is not null)
            return;

        var coords = args.Target;
        var offset = ent.Comp.Offset;

        for (int x = -1 + offset.X; x < ent.Comp.Size.X; x++)
        {
            for (int y = -1 + offset.Y; y < ent.Comp.Size.Y; y++)
            {
                var spawnAt = new EntityCoordinates(coords.EntityId, coords.X + x, coords.Y + y);
                PredictedSpawnAtPosition(ent.Comp.SpawnPrototype,  spawnAt);
            }
        }

        args.Handled = true;
    }
}

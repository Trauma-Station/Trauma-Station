// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Coordinates;

namespace Content.Trauma.Shared.InitPos;

public sealed partial class InitPosSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void Startup(Entity<InitPosComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Coordinates = _transform.GetMoverCoordinates(ent.Owner);
    }
}

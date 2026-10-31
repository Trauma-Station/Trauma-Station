// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Coordinates;

namespace Content.Trauma.Shared.InitPos;

public sealed partial class InitPosSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<InitPosComponent> ent, ref MapInitEvent args)
    {
        var coords = _transform.GetMapCoordinates(ent.Owner); // holy shitcode
        Log.Info($"{coords}");
        Log.Info($"{coords.MapId}");
        Log.Info($"{coords.Position}");
        var cd = _transform.ToCoordinates(coords);
        Log.Info($"{cd}");
        Log.Info($"{cd.Position}");

        ent.Comp.Coordinates = cd;
    }
}

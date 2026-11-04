// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.InitPos;

/// <summary>
/// Stores ent's init location, map and grid included.
/// </summary>
public sealed partial class InitPosSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Startup(Entity<InitPosComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Coordinates = Transform(ent).Coordinates;
        Log.Info($"{ent.Comp.Coordinates}");
        Dirty(ent.Owner, ent.Comp);
    }
}

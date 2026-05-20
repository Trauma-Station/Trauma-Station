// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Tabletop;
using Content.Trauma.Shared.Viewcone.Components;

namespace Content.Trauma.Shared.Viewcone;

/// <summary>
/// Removes viewcone components from tabletop holograms.
/// Lets chess be played.
/// </summary>
public sealed partial class ViewconeTabletopSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ViewconeComponent, TabletopSpawnedEvent>(OnSpawned);
        SubscribeLocalEvent<ViewconeOccludableComponent, TabletopSpawnedEvent>(OnSpawned);
    }

    private void OnSpawned<T>(Entity<T> ent, ref TabletopSpawnedEvent args) where T : Component
    {
        RemComp(ent, ent.Comp);
    }
}

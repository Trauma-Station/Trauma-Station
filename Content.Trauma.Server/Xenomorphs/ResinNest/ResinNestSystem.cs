// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Buckle.Components;
using Robust.Shared.GameObjects;
using Content.Shared._White.ResinNest;

namespace Content.Server._White.ResinNest;

public sealed class ResinNestSystem : EntitySystem
{
        public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ResinNestComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<ResinNestComponent, UnstrappedEvent>(OnUnstrapped);
    }
    private void OnStrapped(Entity<ResinNestComponent> ent, ref StrappedEvent args)
    {
        var overlay = Spawn(ent.Comp.OverlayEntity, Transform(ent).Coordinates);
        ent.Comp.SpawnedOverlay = overlay; //there should really be a way to yaml this kind of shit
    }
    private void OnUnstrapped(Entity<ResinNestComponent> ent, ref UnstrappedEvent args)
    {
        if (ent.Comp.SpawnedOverlay is { } overlay)
            QueueDel(overlay);
        ent.Comp.SpawnedOverlay = null;
    }
}

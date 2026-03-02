// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DragDrop;

namespace Content.Trauma.Shared.Buckle;

public sealed class BuckleableSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<BuckleableComponent, CanDragEvent>(OnDrag);
    }

    private void OnDrag(Entity<BuckleableComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Physics.Events;

namespace Content.Trauma.Shared.BloodCult.BloodBoilProjectile;

public sealed partial class BloodBoilProjectileSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void CheckCollision(Entity<BloodBoilProjectileComponent> ent, ref PreventCollideEvent args)
    {
        args.Cancelled |= args.OtherEntity != ent.Comp.Target;
    }
}

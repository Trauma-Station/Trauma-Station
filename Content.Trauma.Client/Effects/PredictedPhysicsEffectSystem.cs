// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Physics;

namespace Content.Trauma.Client.Effects;

public sealed partial class PredictedPhysicsEffectSystem : EntitySystem
{
    [Dependency] private PhysicsSystem _physics = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<PredictedPhysicsEffectComponent> ent, ref ComponentInit args)
    {
        _physics.UpdateIsPredicted(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnUpdateIsPredicted(Entity<PredictedPhysicsEffectComponent> ent, ref UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }
}

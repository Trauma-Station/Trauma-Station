// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Throwing;
using Content.Trauma.Common.Throwing;
using Robust.Client.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Trauma.Client.Throwing;

/// <summary>
/// Lets thrown items and projectiles' physics be predicted.
/// </summary>
public sealed class PredictedThrowingSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PredictedThrownItemComponent, ComponentStartup>(UpdatePredicted);
        SubscribeLocalEvent<PredictedThrownItemComponent, ComponentShutdown>(UpdatePredicted);
        SubscribeLocalEvent<PredictedThrownItemComponent, UpdateIsPredictedEvent>(OnUpdateIsPredicted);
    }

    private void OnUpdateIsPredicted(Entity<PredictedThrownItemComponent> ent, ref UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }

    private void UpdatePredicted(EntityUid uid, PredictedThrownItemComponent comp, EntityEventArgs args)
    {
        // start/stop predicting physics when added/removed
        _physics.UpdateIsPredicted(uid);
    }
}

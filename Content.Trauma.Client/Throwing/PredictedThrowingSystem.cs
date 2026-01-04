// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Throwing;
using Content.Trauma.Common.Throwing;
using Robust.Client.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

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

        SubscribeLocalEvent<PredictedThrownItemComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PredictedThrownItemComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PredictedThrownItemComponent, UpdateIsPredictedEvent>(OnUpdateIsPredicted);
    }

    private void OnUpdateIsPredicted(Entity<PredictedThrownItemComponent> ent, ref UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }

    private void OnStartup(Entity<PredictedThrownItemComponent> ent, ref ComponentStartup args)
    {
        // start predicted physics immediately
        _physics.UpdateIsPredicted(ent.Owner);
    }

    private void OnShutdown(Entity<PredictedThrownItemComponent> ent, ref ComponentShutdown args)
    {
        // stop predicted physics after a brief delay so it doesn't rubber band with client-server ping
        Timer.Spawn(1000, () => _physics.UpdateIsPredicted(ent.Owner));
    }
}

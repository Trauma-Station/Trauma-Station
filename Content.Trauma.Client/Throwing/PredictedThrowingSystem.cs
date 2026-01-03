// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Robust.Client.Physics;

namespace Content.Trauma.Client.Throwing;

/// <summary>
/// Lets thrown items and projectiles' physics be predicted.
/// </summary>
public sealed class PredictedThrowingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThrownItemComponent, UpdateIsPredictedEvent>(OnUpdateIsPredicted);
        SubscribeLocalEvent<ProjectileComponent, UpdateIsPredictedEvent>(OnUpdateIsPredicted);
    }

    private void OnUpdateIsPredicted(EntityUid uid, IComponent comp, ref UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }
}

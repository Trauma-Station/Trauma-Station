// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Projectiles;

/// <summary>
///     Event raised on entities that have been hit.
/// </summary>
public sealed class HitByProjectileEvent : EntityEventArgs
{
    /// <summary>
    ///     Entity used to attack, for broadcast purposes.
    /// </summary>
    public EntityUid Projectile { get; }

    /// <summary>
    ///     Entity that got hit.
    /// </summary>
    public EntityUid Target { get; }

    public HitByProjectileEvent(EntityUid projectile, EntityUid target)
    {
        Projectile = projectile;
        Target = target;
    }
}

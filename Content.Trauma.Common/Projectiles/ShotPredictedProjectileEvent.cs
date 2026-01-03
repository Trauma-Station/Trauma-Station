// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.Serialization;

namespace Content.Trauma.Common.Projectiles;

/// <summary>
/// Event sent to the client that shot a predicted projectile.
/// Used to hide the server-spawned one.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShotPredictedProjectileEvent : EntityEventArgs
{
    public NetEntity Projectile;
}

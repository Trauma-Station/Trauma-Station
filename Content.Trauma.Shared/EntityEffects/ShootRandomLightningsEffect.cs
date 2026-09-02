// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects.Effects;

/// <summary>
/// Effect that shoot lightnings(like from tesla) from the target entity
/// </summary>
public sealed partial class ShootRandomLightnings : EntityEffectBase<ShootRandomLightnings>
{
    /// <summary>
    /// Up to how far to teleport the user in tiles.
    /// </summary>
    [DataField]
    public float LightningRange = 5f;

    /// <summary>
    /// How many times to try to pick the destination. Larger number means the teleport is more likely to be safe.
    /// </summary>
    [DataField]
    public int LightningBoltCount = 5;
}

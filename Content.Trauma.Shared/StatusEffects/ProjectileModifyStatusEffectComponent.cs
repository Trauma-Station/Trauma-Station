// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.StatusEffects;

/// <summary>
/// Status effect that modifies the damage of the projectile when you shoot it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ProjectileModifyStatusEffectComponent : Component
{
    /// <summary>
    /// The modifier to apply to the projectile's damage.
    /// </summary>
    [DataField(required: true)]
    public float Modifier;

    /// <summary>
    /// If true, it will only apply to reflective projectiles (like lasers).
    /// </summary>
    [DataField]
    public bool Laser;
}

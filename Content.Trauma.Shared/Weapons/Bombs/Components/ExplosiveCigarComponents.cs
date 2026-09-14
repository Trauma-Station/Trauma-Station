// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Trauma.Shared.Weapons.Bombs.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ExplosiveCigarComponent : Component
{
    /// <summary>
    /// How much solution must remain before it explodes.
    /// If null, explodes when the solution is fully empty.
    /// </summary>
    [DataField]
    public float? TriggerAtRemaining = null;

    /// <summary>
    /// Extra damage applied to the wearer's head when exploding while equipped in the mask slot.
    /// </summary>
    [DataField]
    public DamageSpecifier? MaskSlotHeadDamage = null;

    [DataField]
    public TimeSpan NextCheck = TimeSpan.Zero;

    [DataField, AutoPausedField]
    public TimeSpan CheckInterval = TimeSpan.FromSeconds(3);
}

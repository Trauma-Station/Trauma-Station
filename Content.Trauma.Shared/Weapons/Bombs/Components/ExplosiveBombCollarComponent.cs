using Content.Shared.Damage;

namespace Content.Trauma.Shared.Weapons.Bombs.Components;

[RegisterComponent]
public sealed partial class ExplosiveBombCollarComponent : Component
{
    /// <summary>
    /// Extra damage applied to the wearer's head when exploding while equipped in the neck slot.
    /// </summary>
    [DataField]
    public DamageSpecifier? NeckSlotHeadDamage = null;

    /// <summary>
    /// When to apply neck-slot head damage.
    /// </summary>
    [DataField]
    public TimeSpan? ApplyHeadDamageAt;
}

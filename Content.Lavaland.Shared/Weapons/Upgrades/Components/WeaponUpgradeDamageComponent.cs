using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Lavaland.Shared.Weapons.Upgrades.Components;

/// <summary>
/// A <see cref="GunUpgradeComponent"/> for increasing the damage of a gun's projectile.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GunUpgradeSystem))]
public sealed partial class WeaponUpgradeDamageComponent : Component
{
    [DataField]
    public DamageSpecifier? BonusDamage;

    /// <summary>
    /// How much should we multiply the total projectile's damage.
    /// </summary>
    [DataField]
    public float Modifier = 1f;
}

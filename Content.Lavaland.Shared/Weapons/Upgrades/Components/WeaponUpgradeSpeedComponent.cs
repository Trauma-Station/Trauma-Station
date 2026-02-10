using Robust.Shared.GameStates;

namespace Content.Lavaland.Shared.Weapons.Upgrades.Components;

/// <summary>
/// Improves attack rate of melee weapon.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WeaponUpgradeSpeedComponent : Component
{
    [DataField]
    public float? BonusAttackRate;

    [DataField]
    public float? AttackRateMultiplier;
}

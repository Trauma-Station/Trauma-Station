using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised when a light attack is made.
/// </summary>
[Serializable, NetSerializable]
public sealed class LightAttackEvent : AttackEvent
{
    // <Trauma>
    public readonly bool IsLeftClick;
    public readonly bool CanParry;
    // </Trauma>
    public readonly NetEntity? Target;
    public readonly NetEntity Weapon;

    public LightAttackEvent(NetEntity? target, NetEntity weapon, NetCoordinates coordinates, bool isLeftClick = true, bool canParry = true) : base(coordinates) // Trauma - isLeftClick and canParry
    {
        // <Trauma>
        IsLeftClick = isLeftClick;
        CanParry = canParry;
        // </Trauma>
        Target = target;
        Weapon = weapon;
    }
}

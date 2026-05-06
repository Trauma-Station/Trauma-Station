using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised when a light attack is made.
/// </summary>
[Serializable, NetSerializable]
public sealed class LightAttackEvent : AttackEvent
{
    public readonly NetEntity? Target;
    public readonly NetEntity Weapon;
    public readonly bool IsLeftClick; // Trauma

    public LightAttackEvent(NetEntity? target, NetEntity weapon, NetCoordinates coordinates, bool isLeftClick = true) : base(coordinates) // Trauma - isLeftClick
    {
        Target = target;
        Weapon = weapon;
        IsLeftClick = isLeftClick; // Trauma
    }
}

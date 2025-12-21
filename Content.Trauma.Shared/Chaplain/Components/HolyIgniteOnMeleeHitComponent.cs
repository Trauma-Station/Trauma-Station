using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Chaplain.Components;

/// <summary>
/// Component that can be used to add (or remove) holy fire stacks when used as a melee weapon.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HolyIgniteOnMeleeHitComponent : Component
{
    [DataField]
    public float FireStacks;
}

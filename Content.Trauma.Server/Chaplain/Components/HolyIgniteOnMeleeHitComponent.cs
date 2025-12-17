/// <summary>
/// Component that can be used to add (or remove) holy fire stacks when used as a melee weapon.
/// </summary>
namespace Content.Trauma.Server.Chaplain.Components;

[RegisterComponent]
public sealed partial class HolyIgniteOnMeleeHitComponent : Component
{
    [DataField]
    public float FireStacks { get; set; }
}

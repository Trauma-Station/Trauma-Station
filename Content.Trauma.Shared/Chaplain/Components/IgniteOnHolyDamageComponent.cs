using Content.Goobstation.Maths.FixedPoint;

namespace Content.Trauma.Shared.Chaplain.Components;

[RegisterComponent]
public sealed partial class IgniteOnHolyDamageComponent : Component
{
    [DataField("fireStacks")]
    public float FireStacks = 1f;

    // The minimum amount of damage taken to apply fire stacks
    [DataField("threshold")]
    public FixedPoint2 Threshold = 15;
}

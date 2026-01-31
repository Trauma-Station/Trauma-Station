using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Chaplain.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class IgniteOnHolyDamageComponent : Component
{
    /// <summary>
    /// How much fire stacks to apply on damage dealt.
    /// </summary>
    [DataField]
    public float FireStacks = 1f;

    /// <summary>
    /// Gets or sets the minimum amount of damage required to apply fire stacks.
    /// </summary>
    [DataField]
    public FixedPoint2 Threshold = 15;
}

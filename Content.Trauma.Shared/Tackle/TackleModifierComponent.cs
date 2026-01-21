using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Tackle;

[RegisterComponent, NetworkedComponent]
public sealed partial class TackleModifierComponent : Component
{
    [DataField]
    public float SpeedMultiplier = 1f;

    [DataField]
    public float RangeMultiplier = 1f;

    [DataField]
    public float MinDistanceModifier;
}

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Specialization.Components;

/// <summary>
/// Stores specialization shit
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpecializationComponent : Component
{
    /// <summary>
    /// Entity type specialized in.
    /// </summary>
    [DataField]
    public EntProtoId Target;

    /// <summary>
    /// Attack Bonus
    /// </summary>
    [DataField]
    public int Attack;

    /// <summary>
    /// Defense Bonus
    /// </summary>
    [DataField]
    public int Defense;

    /// <summary>
    /// Speed Bonus
    /// </summary>
    [DataField]
    public int Speed;
}

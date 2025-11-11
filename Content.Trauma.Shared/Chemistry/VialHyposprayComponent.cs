using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Chemistry;

/// <summary>
/// Uses a vial item slot for a hypospray's solution.
/// The vial must use <c>DrawableSolutionComponent</c> to work.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(VialHyposprayComponent))]
public sealed partial class VialHyposprayComponent : Component
{
    /// <summary>
    /// The name of the item slot that holds a vial.
    /// </summary>
    [DataField(required: true)]
    public string Slot = string.Empty;
}

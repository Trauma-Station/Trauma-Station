using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Body.Organ;

/// <summary>
/// Component for organs which adds a string to the detailed health examine.
/// This should only be used for something you'd easily notice at a glance.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(OrganExamineSystem))]
public sealed partial class OrganExamineComponent : Component
{
    /// <summary>
    /// Loc string that gets passed:
    /// - target, the identity entity of the target
    /// - part, the BodyPartType this organ is inside
    /// </summary>
    [DataField(required: true)]
    public LocId Examine;
}

using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Adds actions to an organ's body.
/// Actions are removed while disabled by e.g. cybernetic emp.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(OrganActionsSystem))]
public sealed partial class OrganActionsComponent : Component
{
    /// <summary>
    /// The ID of every action to add.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId<ActionComponent>> Actions = new();
}

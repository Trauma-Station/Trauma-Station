using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Attribute.Components;

/// <summary>
/// Stores information about an attribute, assigned
/// to a dummy entity that is parented to some entity with <see cref="KnowledgeContainerComponent"/>, usually a brain.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, EntityCategory("Knowledge")]
public sealed partial class AttributeComponent : Component
{
    /// <summary>
    /// Stores the attribute of whatever.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 Inherent;

    /// <summary>
    /// Temporary attribute that are granted by certain equipment or statuses.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Temporary;

    /// <summary>
    /// The combined inherent + temporary value.
    /// </summary>
    [ViewVariables]
    public FixedPoint2 Attribute => Inherent + Temporary;

    /// <summary>
    /// Can the attribute ever be removed.
    /// </summary>
    [DataField]
    public bool Unremoveable = false;
}

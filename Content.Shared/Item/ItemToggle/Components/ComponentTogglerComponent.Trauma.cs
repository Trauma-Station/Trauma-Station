using Robust.Shared.Prototypes;

namespace Content.Shared.Item.ItemToggle.Components;

public sealed partial class ComponentTogglerComponent
{
    /// <summary>
    /// The components to add deactivated.
    /// </summary>
    [DataField]
    public ComponentRegistry DeactivateComponents = new();
}

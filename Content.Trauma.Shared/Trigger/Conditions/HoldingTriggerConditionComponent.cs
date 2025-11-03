using Content.Shared.Trigger.Components.Conditions;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Trigger.Conditions;

/// <summary>
/// Requires that the user is holding this item to trigger it, depending on <see cref="Holding"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HoldingTriggerConditionComponent : BaseTriggerConditionComponent
{
    /// <summary>
    /// Whether the user must be holding the item or not.
    /// </summary>
    [DataField]
    public bool Holding = true;
}

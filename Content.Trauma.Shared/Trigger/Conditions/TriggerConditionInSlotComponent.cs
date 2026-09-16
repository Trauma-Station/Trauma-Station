namespace Content.Trauma.Shared.Trigger.Conditions;

/// <summary>
/// Trigger condition: the entity must be inside the given inventory slot.
/// </summary>
[RegisterComponent]
public sealed partial class TriggerConditionInSlotComponent : Component
{
    /// <summary>
    /// Inventory slot name, ex: "neck", "mask".
    /// </summary>
    [DataField(required: true)]
    public string Slot = default!;
}

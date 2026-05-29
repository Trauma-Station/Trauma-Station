namespace Content.Shared.Trigger.Components.Effects;

public sealed partial class ShockOnTriggerComponent
{
    /// <summary>
    /// How much battery charge from this entity used when shocking.
    /// Ignores battery if 0.
    /// </summary>
    [DataField]
    public float ShockCharge;
}

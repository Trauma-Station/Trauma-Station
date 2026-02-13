namespace Content.Shared.Chemistry.Components;

public sealed partial class InjectorComponent
{
    /// <summary>
    /// If not null, check if target is within this range before injecting if CanReach check fails
    /// </summary>
    [DataField]
    public float? InteractionRangeOverride;
}

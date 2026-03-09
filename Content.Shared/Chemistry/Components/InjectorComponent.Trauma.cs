namespace Content.Shared.Chemistry.Components;

public sealed partial class InjectorComponent
{
    [DataField]
    public float? InteractionRangeOverride;

    [DataField]
    public bool BreakOnMove = true;
}

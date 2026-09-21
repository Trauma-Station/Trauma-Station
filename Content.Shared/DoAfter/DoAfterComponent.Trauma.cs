namespace Content.Shared.DoAfter;

public sealed partial class DoAfterComponent
{
    /// <summary>
    /// Whether to raise <c>DoAfterEndedEvent</c> on the user after it ends.
    /// </summary>
    [DataField]
    public bool RaiseEndedEvent;
}

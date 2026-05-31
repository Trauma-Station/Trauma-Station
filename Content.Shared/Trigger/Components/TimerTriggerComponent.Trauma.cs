namespace Content.Shared.Trigger.Components;

public sealed partial class TimerTriggerComponent
{
    /// <summary>
    /// If true, the timer's next activation will be blocked.
    /// </summary>
    [DataField]
    public bool Disabled = false;
}

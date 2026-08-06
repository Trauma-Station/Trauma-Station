namespace Content.Server.StationEvents.Components;

public sealed partial class AlertLevelInterceptionRuleComponent
{
    /// <summary>
    /// Whether or not to override the current alert level, if it isn't green.
    /// </summary>
    [DataField]
    public bool OverrideAlert;

    /// <summary>
    /// Whether the alert level should be changeable.
    /// </summary>
    [DataField]
    public bool Locked;
}

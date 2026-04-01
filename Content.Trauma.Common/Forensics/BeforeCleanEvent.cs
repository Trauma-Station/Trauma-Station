namespace Content.Trauma.Common.Forensics;

/// <summary>
/// Raised on the target being cleaned right before the cleaning DoAfter begins.
/// </summary>
[ByRefEvent]
public record struct BeforeCleanEvent(float CleanDelay);

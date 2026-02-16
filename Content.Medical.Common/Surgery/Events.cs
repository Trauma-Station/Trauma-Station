namespace Content.Medical.Common.Surgery;

/// <summary>
/// Raised on the user to check if a surgery step should cause pain.
/// Also relayed to held items.
/// </summary>
[ByRefEvent]
public record struct SurgeryPainEvent(bool Cancelled = false);

/// <summary>
/// Raised on the user to check if it should ignore required surgeries or let you start the target one immediately.
/// Also relayed to held items.
/// </summary>
[ByRefEvent]
public record struct SurgeryIgnorePreviousStepsEvent(bool Handled = false);

namespace Content.Trauma.Common.Nutrition;

/// <summary>
/// Raised on the mob that ate food just before it gets deleted.
/// User is different from the event target if being force fed.
/// </summary>
[ByRefEvent]
public readonly record struct FullyAteEvent(EntityUid Food, EntityUid User);

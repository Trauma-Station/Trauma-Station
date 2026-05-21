namespace Content.Shared.Prying.Systems;

/// <summary>
/// Raised on the user, checks if user can pry open something.
/// </summary>
[ByRefEvent]
public record struct CheckPryEvent(EntityUid PryingTarget, EntityUid? Tool);

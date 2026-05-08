namespace Content.Trauma.Common.Grudge;


/// <summary>
/// Raised on a grudge. Passes owner and grudgee for informational setup.
/// </summary>
[ByRefEvent]
public record struct GrudgeAddedEvent(EntityUid Accuser, EntityUid Accused, EntityUid AccuserObjective, EntityUid AccusedObjective);

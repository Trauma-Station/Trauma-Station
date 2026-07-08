using Robust.Shared.Map;

namespace Content.Trauma.Common.Interaction;

[ByRefEvent]
public record struct AfterInteractTargetEvent(EntityUid User,
    EntityUid Used,
    EntityUid Target,
    EntityCoordinates ClickLocation,
    bool CanReach,
    bool Handled = false);

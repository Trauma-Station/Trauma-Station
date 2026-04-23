using Content.Shared.Actions;

namespace Content.Goobstation.Shared.SpaceWhale;

[ByRefEvent]
public record struct GetTailedEntitySegmentCountEvent(int Amount);

[ByRefEvent]
public readonly record struct UpdateTailedEntitySegmentCountEvent(int Amount);

public sealed partial class TailedEntityForceContractEvent : InstantActionEvent;

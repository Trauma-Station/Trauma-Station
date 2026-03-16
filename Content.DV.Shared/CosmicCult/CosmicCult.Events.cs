using Robust.Shared.Serialization;

namespace Content.Shared._DV.CosmicCult;

public sealed partial class CosmicSiphonIndicatorEvent() : EntityEventArgs
{
}

public sealed partial class CosmicCultLeadChangedEvent() : EntityEventArgs
{
}

public sealed partial class CosmicCultAddedCultistEvent(): EntityEventArgs
{
}

[ByRefEvent]
public record struct CosmicAbilityAttemptEvent(EntityUid Target, bool PlayEffects = false, bool Cancelled = false);

/// <summary>
///     Event dispatched from shared into server code where something creates another thing that should be associated with the gamerule
/// </summary>
[ByRefEvent]
public record struct CosmicCultAssociateRuleEvent(EntityUid Originator, EntityUid Target);

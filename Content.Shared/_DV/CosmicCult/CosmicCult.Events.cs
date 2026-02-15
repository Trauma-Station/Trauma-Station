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

using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Heretic;

[ByRefEvent]
public readonly record struct ConsumingFoodEvent(EntityUid Food, FixedPoint2 Volume);

[ByRefEvent]
public record struct ImmuneToPoisonDamageEvent(bool Immune = false);

[ByRefEvent]
public readonly record struct SetGhoulBoundHereticEvent(EntityUid Heretic);

[ByRefEvent]
public readonly record struct IncrementHereticObjectiveProgressEvent(EntProtoId Proto, int Amount = 1);

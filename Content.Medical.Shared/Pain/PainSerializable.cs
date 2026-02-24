using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Medical.Shared.Pain;

[Serializable, NetSerializable]
public enum PainDamageTypes : byte
{
    WoundPain,
    TraumaticPain,
}

[Serializable, NetSerializable]
public enum PainThresholdTypes : byte
{
    None,
    PainFlinch,
    Agony,
    PainShock,
    PainShockAndAgony,
}

[Serializable, DataRecord]
public partial record struct PainMultiplier(FixedPoint2 Change, string Identifier = "Unspecified", PainDamageTypes PainDamageType = PainDamageTypes.WoundPain, TimeSpan? Time = null);

[Serializable, DataRecord]
public partial record struct PainFeelingModifier(FixedPoint2 Change, TimeSpan? Time = null);

[Serializable, DataRecord]
public partial record struct PainModifier(FixedPoint2 Change, string Identifier = "Unspecified", PainDamageTypes PainDamageType = PainDamageTypes.WoundPain, TimeSpan? Time = null); // Easier to manage pain with modifiers.

[ByRefEvent]
public partial record struct PainThresholdTriggered(Entity<NerveSystemComponent> NerveSystem, PainThresholdTypes ThresholdType, FixedPoint2 PainInput, bool Cancelled = false);

[ByRefEvent]
public partial record struct PainThresholdEffected(Entity<NerveSystemComponent> NerveSystem, PainThresholdTypes ThresholdType, FixedPoint2 PainInput);

[ByRefEvent]
public record struct PainFeelsChangedEvent(EntityUid NerveSystem, EntityUid NerveEntity, FixedPoint2 CurrentPainFeels);
[ByRefEvent]
public record struct PainModifierAddedEvent(EntityUid NerveSystem, EntityUid NerveUid, FixedPoint2 AddedPain);

[ByRefEvent]
public record struct PainModifierRemovedEvent(EntityUid NerveSystem, EntityUid NerveUid, FixedPoint2 CurrentPain);

[ByRefEvent]
public record struct PainModifierChangedEvent(EntityUid NerveSystem, EntityUid NerveUid, FixedPoint2 CurrentPain);

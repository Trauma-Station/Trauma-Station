using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.MartialArts;

[ByRefEvent]
public record struct ComboBeingPerformedEvent(ProtoId<ComboPrototype> Combo);

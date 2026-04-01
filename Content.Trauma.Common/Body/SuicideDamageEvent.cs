using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Body;

[ByRefEvent]
public record struct SuicideDamageEvent(ProtoId<DamageTypePrototype> DamageType);

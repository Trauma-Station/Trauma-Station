// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Body;

[ByRefEvent]
public record struct SuicideDamageEvent(ProtoId<DamageTypePrototype> DamageType);

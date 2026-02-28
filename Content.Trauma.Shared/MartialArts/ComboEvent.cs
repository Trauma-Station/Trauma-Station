// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.MartialArts;

[ByRefEvent]
public record struct ComboEvent(ProtoId<ComboPrototype> Combo);

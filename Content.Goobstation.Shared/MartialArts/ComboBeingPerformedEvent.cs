// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.MartialArts;

[ByRefEvent]
public record struct ComboBeingPerformedEvent(ProtoId<ComboPrototype> ProtoId);

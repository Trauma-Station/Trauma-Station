// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.InternalResources.Events;

[ByRefEvent]
public record struct GetInternalResourcesCostModifierEvent(EntityUid Target, float Multiplier = 1);

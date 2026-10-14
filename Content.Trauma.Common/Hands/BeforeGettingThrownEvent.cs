// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Hands;

[ByRefEvent]
public record struct BeforeGettingThrownEvent(EntityUid User, bool Cancelled = false);

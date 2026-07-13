// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Throwing;

[ByRefEvent]
public record struct DamageOtherOnHitAttemptEvent(bool Cancelled = false);

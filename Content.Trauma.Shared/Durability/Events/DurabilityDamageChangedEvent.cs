// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.Durability.Events;

[ByRefEvent]
public record struct DurabilityChangeAttemptEvent(EntityUid Uid, FixedPoint2 Damage);

[ByRefEvent]
public record struct DurabilityDamageChangedEvent(EntityUid Uid, FixedPoint2 Damage, FixedPoint2 OldDamage);

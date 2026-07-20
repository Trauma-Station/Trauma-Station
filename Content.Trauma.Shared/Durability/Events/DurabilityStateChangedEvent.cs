// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Durability.Events;

// Raised on Weapon
[ByRefEvent]
public record struct DurabilityStateChangedEvent(DurabilityState OldState, DurabilityState NewState, EntityUid Weapon, EntityUid? Attacker = null, HashSet<EntityUid>? Targets = null, EntityUid? Used = null);

// Raised on Used
[ByRefEvent]
public record struct DurabilityStateChangedByEvent(DurabilityState OldState, DurabilityState NewState, EntityUid Weapon, EntityUid? Attacker = null, HashSet<EntityUid>? Targets = null, EntityUid? Used = null);

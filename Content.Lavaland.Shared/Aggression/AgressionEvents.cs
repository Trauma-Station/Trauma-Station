// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Lavaland.Shared.Aggression;

/// <summary>
/// Raised on the entity with AggressiveComponent when it added new aggressor.
/// </summary>
[ByRefEvent]
public record struct AggressorAddedEvent(EntityUid Aggressor);

/// <summary>
/// Raised on the entity with AggressiveComponent when it removed one of it's aggressors.
/// </summary>
[ByRefEvent]
public record struct AggressorRemovedEvent(EntityUid Aggressor);

/// <summary>
/// Raised on the aggressor when a new aggressive is added to it.
/// </summary>
[ByRefEvent]
public record struct AggressiveAddedEvent(EntityUid Aggressive);

/// <summary>
/// Raised on the aggressor when the last aggressive entity is being removed and the component is about to get deleted.
/// </summary>
[ByRefEvent]
public record struct AggressiveRemovedEvent(EntityUid Aggressive);

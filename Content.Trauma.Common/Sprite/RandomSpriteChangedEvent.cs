// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.Sprite;

/// <summary>
/// Event raised on an entity with RandomSprite if its colour changes.
/// Used for part skin colouring, only matters for mobs.
/// </summary>
[ByRefEvent]
public record struct RandomSpriteChangedEvent();

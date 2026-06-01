// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.CombatMode;

/// <summary>
/// Raised on the player entity before the <see cref="CombatModeIndicatorsOverlay"/> draws the sight texture,
/// in order to override it with a custom one.
/// </summary>
[ByRefEvent]
public record struct GetCombatModeCursorEvent(SpriteSpecifier? Sprite);

// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Popups;

/// <summary>
/// Raised on the client's local entity before trying to render a popup at a world position.
/// </summary>
[ByRefEvent]
public record struct ShowPopupAttemptEvent(Vector2 WorldPos, Vector2 ViewerPos, bool Cancelled = false);

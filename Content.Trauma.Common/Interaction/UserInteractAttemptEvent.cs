// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Trauma.Common.Interaction;

/// <summary>
/// Raised on the user before left-click interaction with an entity of any kind takes place.
/// Handle to skip regular interaction logic.
/// </summary>
[ByRefEvent]
public record struct UserInteractAttemptEvent(EntityUid User,
    EntityUid? Target,
    EntityCoordinates ClickLocation,
    bool CanReach,
    bool Handled = false);

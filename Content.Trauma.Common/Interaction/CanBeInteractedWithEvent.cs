// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Trauma.Common.Interaction;

/// <summary>
/// Raised on target before AfterInteractEvent, used to block interaction with and object
/// Used for low-priority interactions facilitated by the used entity.
/// </summary>
[ByRefEvent]
public record struct CanBeInteractedWithEvent(EntityUid User,
    EntityUid Used,
    EntityUid Target,
    EntityCoordinates ClickLocation,
    bool CanReach,
    bool Handled = false);

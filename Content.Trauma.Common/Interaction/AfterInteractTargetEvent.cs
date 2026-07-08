// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Trauma.Common.Interaction;

[ByRefEvent]
public record struct AfterInteractTargetEvent(EntityUid User,
    EntityUid Used,
    EntityUid Target,
    EntityCoordinates ClickLocation,
    bool CanReach,
    bool Handled = false);

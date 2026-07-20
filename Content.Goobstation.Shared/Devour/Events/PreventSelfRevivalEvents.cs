// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Devour.Events;

[ByRefEvent]
public record struct BeforeSelfRevivalEvent(
    EntityUid Target,
    LocId PopupText,
    bool Handled = false,
    bool Cancelled = false);

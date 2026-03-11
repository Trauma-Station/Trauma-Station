// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Medical.Shared.Surgery;

[ByRefEvent]
public record struct SurgerySanitizationEvent(bool Handled = false);

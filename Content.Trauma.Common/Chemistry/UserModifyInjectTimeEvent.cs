// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Chemistry;

/// <summary>
/// Raised on the user when injecting into a mob with syringe, hypo, etc.
/// </summary>
[ByRefEvent]
public record struct UserModifyInjectTimeEvent(EntityUid User, EntityUid Injector, TimeSpan Delay);

// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.Chemistry;

/// <summary>
/// Raised on an injector when it successfully injects a target.
/// </summary>
[ByRefEvent]
public readonly record struct AfterInjectedEvent(EntityUid User, EntityUid Target);

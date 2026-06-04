// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Prying;

/// <summary>
/// Raised on the user, checks if user can pry open something.
/// </summary>
[ByRefEvent]
public record struct CheckPryEvent(EntityUid PryingTarget, EntityUid? Tool = null, bool Pry = false);

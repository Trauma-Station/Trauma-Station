// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Common.Cuffs;

/// <summary>
/// Raised on the user to see if it can uncuff instantly.
/// </summary>
[ByRefEvent]
public record struct InstantUncuffEvent(EntityUid Target, EntityUid Cuff, bool CuffsBroken = false);

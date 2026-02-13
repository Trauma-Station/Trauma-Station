// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.CardboardBox;

/// <summary>
/// Event raised on a cardboard box to prevent enabling stealth for it.
/// </summary>
[ByRefEvent]
public record struct BoxStealthAttemptEvent(bool Cancelled = false);

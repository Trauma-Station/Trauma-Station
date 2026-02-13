// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.CardboardBox;

/// <summary>
/// Event raised on a cardboard box to prevent the alert sound+effect.
/// </summary>
[ByRefEvent]
public record struct BoxAlertAttemptEvent(bool Cancelled = false);

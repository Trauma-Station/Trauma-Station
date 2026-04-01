// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Voting;

/// <summary>
/// Raised on the player entity when checking voting eligibility. If the event is cancelled, the player will not be able to vote.
/// </summary>

[ByRefEvent]
public record struct CheckVotingEligibilityEvent(bool Cancelled = false);

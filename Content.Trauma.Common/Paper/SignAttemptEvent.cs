// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.Paper;

/// <summary>
/// Raised on the paper when someone tries to sign it.
/// </summary>
[ByRefEvent]
public record struct SignAttemptEvent(EntityUid Signer, bool Cancelled = false);

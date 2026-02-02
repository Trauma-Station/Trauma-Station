// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.Paper;

/// <summary>
/// Raised on the signer to allow overriding their signature.
/// </summary>
[ByRefEvent]
public record struct GetSignatureEvent(EntityUid Paper, string? Signature = null);

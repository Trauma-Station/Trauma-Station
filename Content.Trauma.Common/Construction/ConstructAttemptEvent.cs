// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Construction;

/// <summary>
/// Raised on the user when trying to start a construction.
/// If <c>LogError</c> is true, it should log an error, when clients send malicious input to the server.
/// </summary>
[ByRefEvent]
public record struct ConstructAttemptEvent(EntityUid user, string Prototype, bool LogError = true, bool Cancelled = false);

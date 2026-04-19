// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.Cuffs;

/// <summary>
/// Raised on the user whenever the user gets out of a snare.
/// </summary>
[ByRefEvent]
public record struct EnsnareBrokenEvent(EntityUid? Target);

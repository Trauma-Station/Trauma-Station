// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.MartialArts;

/// <summary>
/// Raised on the martial art entity to allow preventing combos from being built with moves.
/// </summary>
[ByRefEvent]
public record struct ComboAttemptEvent(bool Cancelled = false);

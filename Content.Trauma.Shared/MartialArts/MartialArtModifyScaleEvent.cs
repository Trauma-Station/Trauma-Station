// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.MartialArts;

/// <summary>
/// Raised on a martial art entity to change the scale of its effects.
/// </summary>
[ByRefEvent]
public record struct MartialArtModifyScaleEvent(EntityUid User, float Scale = 1f);

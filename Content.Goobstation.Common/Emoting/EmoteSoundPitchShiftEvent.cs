// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.Emoting;

/// <summary>
/// Used by <see cref="CatEmoteSpamCountermeasureSystem"/> to pitch emote sounds as it nears to a smite
/// </summary>
/// <param name="pitch">additive pitch to the sound</param>
[ByRefEvent]
public record struct EmoteSoundPitchShiftEvent(float Pitch = 0);

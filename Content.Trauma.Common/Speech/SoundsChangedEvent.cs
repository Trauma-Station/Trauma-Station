// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Speech;

/// <summary>
///     Raised when the vocal component's Sounds field changes.
/// </summary>
[ByRefEvent]
public record struct EmoteSoundsChangedEvent();

/// <summary>
///     Raised when the speech component's Sounds field changes.
/// </summary>
[ByRefEvent]
public record struct SpeechSoundsChangedEvent();

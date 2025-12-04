// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.Speech;

/// <summary>
/// Event raised on a speech source to allow replacing the font used.
/// This overrides languages.
/// </summary>
[ByRefEvent]
public record struct SpeechFontOverrideEvent(EntityUid Source, string Font);

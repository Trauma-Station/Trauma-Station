// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Speech.Components;

namespace Content.Trauma.Shared.Speech;

/// <summary>
/// Replaces individual characters with random strings, ignoring case etc.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CharactersAccentSystem))]
public sealed partial class CharactersAccentComponent : BaseAccentComponent
{
    [DataField(required: true)]
    public Dictionary<char, List<string>> Chars = new();
}

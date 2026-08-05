// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Trauma.Shared.SpeechPro;

namespace Content.Trauma.Client.SpeechPro.UI;

[GenerateTypedNameReferences]
public sealed partial class SpeechProWindow : FancyWindow
{
    public event Action<ProtoId<SpeechProPhrasePrototype>>? OnPhraseSelected;

    public SpeechProWindow()
    {
        RobustXamlLoader.Load(this);

        SpeechProFragment.OnPhraseSelected += phrase => OnPhraseSelected?.Invoke(phrase);
    }
}

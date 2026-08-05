// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.SpeechPro;

namespace Content.Trauma.Client.SpeechPro.UI;

public sealed class SpeechProBoundUserInterface : BoundUserInterface
{
    private SpeechProWindow? _window;

    public SpeechProBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SpeechProWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window.OnPhraseSelected += phrase => SendMessage(new SpeechProUiMessage(phrase));
    }
}

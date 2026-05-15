// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Antag;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Antag.UI;

public sealed partial class AntagSummonerBUI : BoundUserInterface
{
    private AntagSummonerWindow _window;

    public AntagSummonerBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _window = this.CreateWindow<AntagSummonerWindow>();
        if (EntMan.TryGetComponent<AntagSummonerComponent>(owner, out var comp))
            _window.SetComp(comp);
        _window.OnSummonPressed += () =>
        {
            SendPredictedMessage(new SummonAntagMessage());
            Close();
        };
    }

    protected override void Open()
    {
        base.Open();

        _window.OpenCentered();
    }
}

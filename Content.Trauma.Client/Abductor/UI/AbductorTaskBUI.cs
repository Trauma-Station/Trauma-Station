// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Abductor;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Abductor.UI;

public sealed class AbductorTaskBUI : BoundUserInterface
{
    private AbductorTaskWindow _window;

    public AbductorTaskBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _window = this.CreateWindow<AbductorTaskWindow>();
        _window.SetOwner(owner);
        _window.OnScan += () => SendPredictedMessage(new AbductorTaskScanMessage());
        _window.OnComplete += () => SendPredictedMessage(new AbductorTaskCompleteMessage());
    }

    protected override void Open()
    {
        base.Open();

        _window.OpenCentered();
    }
}

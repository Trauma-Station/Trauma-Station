// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Bitrunning;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Bitrunning.UI;

[UsedImplicitly]
public sealed class QuantumConsoleBUI : BoundUserInterface
{
    private QuantumConsoleWindow? _window;

    public QuantumConsoleBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<QuantumConsoleWindow>();
        _window.OnLoadDomain += id => SendPredictedMessage(new QuantumConsoleLoadDomainMessage(id));
        _window.OnRandomDomain += () => SendPredictedMessage(new QuantumConsoleRandomDomainMessage());
        _window.OnStopDomain += () => SendPredictedMessage(new QuantumConsoleStopDomainMessage());
        _window.OnRefresh += () => SendPredictedMessage(new QuantumConsoleRefreshMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is QuantumConsoleBoundUiState cast)
            _window?.UpdateState(cast);
    }
}

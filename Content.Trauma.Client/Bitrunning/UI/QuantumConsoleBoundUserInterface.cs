// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Bitrunning;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Bitrunning.UI;

[UsedImplicitly]
public sealed class QuantumConsoleBoundUserInterface : BoundUserInterface
{
    private QuantumConsoleWindow? _window;

    public QuantumConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<QuantumConsoleWindow>();
        _window.OnLoadDomain += id => SendMessage(new QuantumConsoleLoadDomainMessage(id));
        _window.OnBroadcastToggle += enabled => SendMessage(new QuantumConsoleBroadcastMessage(enabled));
        _window.OnRandomDomain += () => SendMessage(new QuantumConsoleRandomDomainMessage());
        _window.OnStopDomain += () => SendMessage(new QuantumConsoleStopDomainMessage());
        _window.OnRefresh += () => SendMessage(new QuantumConsoleRefreshMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is QuantumConsoleBoundUiState cast)
            _window?.UpdateState(cast);
    }
}

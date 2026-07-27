// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Bitrunning;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Bitrunning.UI.Netpod;

[UsedImplicitly]
public sealed class NetpodBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private NetpodWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<NetpodWindow>();
        _window.OnSelectLoadout += loadoutId => SendMessage(new NetpodSelectLoadoutMessage(loadoutId));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is NetpodBoundUiState cast)
            _window?.UpdateState(cast);
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Gateway;

namespace Content.Trauma.Client.Gateway.UI;

public sealed class GatewayBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private GatewayWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GatewayWindow>();
        _window.NetOwner = EntMan.GetNetEntity(Owner);
        _window.Owner = Owner;

        _window.OpenPortal += destination =>
        {
            SendMessage(new GatewayOpenPortalMessage(destination));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not GatewayBoundUserInterfaceState current)
            return;

        _window?.UpdateState(current);
    }
}

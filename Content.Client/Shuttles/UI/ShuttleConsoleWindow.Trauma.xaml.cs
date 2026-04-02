// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Shuttles;

namespace Content.Client.Shuttles.UI;

public sealed partial class ShuttleConsoleWindow
{
    public event Action<NetEntity?, InertiaDampeningMode>? OnInertiaDampeningModeChanged;

    public event Action<string>? OnNetworkPortButtonPressed;

    private void NfInitialize()
    {
        NavContainer.OnInertiaDampeningModeChanged += (entity, mode) =>
        {
            OnInertiaDampeningModeChanged?.Invoke(entity, mode);
        };

        NavContainer.OnNetworkPortButtonPressed += (sourcePort) =>
        {
            OnNetworkPortButtonPressed?.Invoke(sourcePort);
        };
    }
}

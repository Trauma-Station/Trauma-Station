// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.GameTicking;

namespace Content.Trauma.Client.UserActions;

public sealed partial class StatusControlSystem : EntitySystem
{
    public string Info = string.Empty;

    public event Action? OnInfoUpdated;

    [SubscribeNetworkEvent]
    private void OnGetInfo(TickerInGameInfoEvent args)
    {
        Info = args.Text;
        OnInfoUpdated?.Invoke();
    }
}

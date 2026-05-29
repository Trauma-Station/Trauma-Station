// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Administration;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared.Database;

namespace Content.Goobstation.Server.Administration.Systems;

public sealed partial class AdminInfoSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IPlayerLocator _locator = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AdminInfoEvent>(OnAdminInfo);
    }

    private async void OnAdminInfo(AdminInfoEvent ev, EntitySessionEventArgs args)
    {
        var name = args.SenderSession.Name;
        if (ev.user == args.SenderSession.UserId)
            return;

        // Try to get original account for this session
        var main = await _locator.LookupIdAsync(ev.user);

        // We don't have a player like that, ignore.
        if (main == null)
            return;

        _adminLog.Add(LogType.AdminMessage, LogImpact.High, $"{name} is attempting to connect with a userid from {main.Username}");
        _chat.SendAdminAlert($"{name} is attempting to connect with a userid from {main.Username}");
    }
}

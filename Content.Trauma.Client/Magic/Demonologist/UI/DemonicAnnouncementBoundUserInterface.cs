// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Trauma.Shared.Magic.Demonologist.UI;
using JetBrains.Annotations;
using Robust.Shared.Configuration;

namespace Content.Trauma.Client.Magic.Demonologist.UI;

[UsedImplicitly]
public sealed partial class DemonicAnnouncementBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IConfigurationManager _cfg = default!;

    private DemonicAnnouncementMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<DemonicAnnouncementMenu>();
        _menu.OnAnnounce += OnAnnounce;
    }

    private void OnAnnounce(string message)
    {
        var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
        var msg = SharedChatSystem.SanitizeAnnouncement(message, maxLength);
        SendMessage(new DemonicAnnouncementMessage(msg));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not DemonicAnnouncementConsoleState announcementState)
            return;

        if (_menu != null)
        {
            _menu.CanAnnounce = announcementState.CanAnnounce;
            _menu.AnnounceButton.Disabled = !announcementState.CanAnnounce;
        }
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Content.Client.UserInterface.Controls;

namespace Content.Trauma.Client.Magic.Demonologist.UI;

[GenerateTypedNameReferences]
public sealed partial class DemonicAnnouncementMenu : FancyWindow
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ILocalizationManager _loc = default!;

    public bool CanAnnounce;

    public event Action<string>? OnAnnounce;

    public DemonicAnnouncementMenu()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

        MessageInput.Placeholder = new Rope.Leaf(_loc.GetString("demonic-announcement-menu-placeholder"));

        var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
        MessageInput.OnTextChanged += (args) =>
        {
            AnnounceButton.Disabled = !CanAnnounce || args.Control.TextLength == 0 || args.Control.TextLength > maxLength;
        };

        AnnounceButton.OnPressed += _ => OnAnnounce?.Invoke(Rope.Collapse(MessageInput.TextRope));
    }
}

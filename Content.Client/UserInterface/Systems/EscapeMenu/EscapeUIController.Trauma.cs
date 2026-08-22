using Content.Shared.CCVar;
using Content.Trauma.Common.LinkAccount;
using Robust.Shared;

namespace Content.Client.UserInterface.Systems.EscapeMenu;

public sealed partial class EscapeUIController
{
    [Dependency] private ILinkAccountManager _linkAccount = default!;

    public Action? OnTogglePatronPerksWindow;
    public Options.UI.EscapeMenu? Window => _escapeWindow;

    private void StateEnteredTrauma(Options.UI.EscapeMenu menu)
    {
        var repo = _cfg.GetCVar(CCVars.InfoLinksGithub);
        menu.SourceCodeButton.Visible = repo != "";
        menu.SourceCodeButton.OnPressed += _ =>
        {
            var commit = _cfg.GetCVar(CVars.BuildVersion);
            if (commit == "") // for dev, live server has it set to commit hash
                commit = "master";
            var uri = $"{repo}/tree/{commit}";
            _uri.OpenUri(uri);
        };

        // TODO: proper ui injection
        menu.PatronPerksButton.Visible = _linkAccount.CanViewPatronPerks();
        menu.PatronPerksButton.OnPressed += _ =>
        {
            CloseEscapeWindow();
            OnTogglePatronPerksWindow?.Invoke();
        };
    }
}

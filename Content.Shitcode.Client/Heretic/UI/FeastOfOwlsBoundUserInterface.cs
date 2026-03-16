using Content.Shared.Heretic;
using Content.Shared.Heretic.Messages;
using Robust.Client.UserInterface;

namespace Content.Client._Shitcode.Heretic.UI;

public sealed class FeastOfOwlsBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    private FeastOfOwlsMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<FeastOfOwlsMenu>();
        _menu.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new FeastOfOwlsMessage(true));
            Close();
        };
        _menu.DenyButton.OnPressed += _ =>
        {
            SendMessage(new FeastOfOwlsMessage(false));
            Close();
        };

        _menu.OpenCentered();
    }
}

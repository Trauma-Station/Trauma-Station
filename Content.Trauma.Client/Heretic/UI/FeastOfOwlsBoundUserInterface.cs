// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Messages;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Heretic.UI;

[UsedImplicitly]
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

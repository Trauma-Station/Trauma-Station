// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Salvage;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Salvage.UI;

public sealed partial class MiningVoucherBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private MiningVoucherMenu? _menu;

    public MiningVoucherBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<MiningVoucherMenu>();
        _menu.SetEntity(Owner);
        _menu.OnSelected += i =>
        {
            SendMessage(new MiningVoucherSelectMessage(i));
            Close();
        };
    }
}

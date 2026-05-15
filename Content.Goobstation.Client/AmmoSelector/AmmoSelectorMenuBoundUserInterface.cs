// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Weapons.AmmoSelector;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;

namespace Content.Goobstation.Client.AmmoSelector;

[UsedImplicitly]
public sealed partial class AmmoSelectorMenuBoundUserInterface : BoundUserInterface
{
    [Dependency] private IClyde _display = default!;
    [Dependency] private IInputManager _input = default!;

    private AmmoSelectorMenu? _menu;

    public AmmoSelectorMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<AmmoSelectorMenu>();
        _menu.SetEntity(Owner);
        _menu.SendAmmoSelectorSystemMessageAction += SendAmmoSelectorSystemMessage;

        var vpSize = _display.ScreenSize;
        _menu.OpenCenteredAt(_input.MouseScreenPosition.Position / vpSize);
    }

    public void SendAmmoSelectorSystemMessage(ProtoId<SelectableAmmoPrototype> protoId)
    {
        SendPredictedMessage(new AmmoSelectedMessage(protoId));
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing.Components;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;

namespace Content.Goobstation.Client.Clothing;

public sealed partial class ToggleableClothingBoundUserInterface : BoundUserInterface
{
    [Dependency] private IClyde _display = default!;
    [Dependency] private IInputManager _input = default!;

    private ToggleableClothingRadialMenu? _menu;

    public ToggleableClothingBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        // TODO: use SimpleRadialMenu for this shit
        _menu = this.CreateWindow<ToggleableClothingRadialMenu>();
        _menu.SetEntity(Owner);
        _menu.SendToggleClothingMessageAction += SendToggleableClothingMessage;

        var vpSize = _display.ScreenSize;
        _menu.OpenCenteredAt(_input.MouseScreenPosition.Position / vpSize);
    }

    private void SendToggleableClothingMessage(EntityUid uid)
    {
        var message = new ToggleableClothingUiMessage(EntMan.GetNetEntity(uid));
        SendPredictedMessage(message);
    }
}

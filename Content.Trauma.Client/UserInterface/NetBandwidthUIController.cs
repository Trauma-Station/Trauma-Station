// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Input;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.UserInterface;

public sealed partial class NetBandwidthUIController : UIController
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        var panel = new NetBandwidthPanel(_timing, _net);
        // shitters making it internal award
        foreach (var child in _ui.RootControl.Children)
        {
            if (child is not DropDownDebugConsole console)
                continue;

            console.BelowConsole.AddChild(panel);
            break;
        }

        _input.SetInputCommand(TraumaKeyFunctions.NetBandwidth,
            InputCmdHandler.FromDelegate(_ => panel.Toggle()));
    }
}

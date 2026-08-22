// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.TextScreen;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Screens;

public sealed partial class SignalScreenSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedDeviceLinkSystem _device = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<SignalScreenComponent> ent, ref ComponentInit args)
    {
        _device.EnsureSinkPorts(ent.Owner, ent.Comp.TextPort);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<SignalScreenComponent> ent, ref SignalReceivedEvent args)
    {
        var now = _timing.CurTime;
        if (now < ent.Comp.NextChange ||
            args.Port != ent.Comp.TextPort ||
            args.Data is not { } data)
            return;

        var text = string.Empty;
        if (data.TryGetValue<string>("logic_string", out var s))
            text = s;
        else if (data.TryGetValue<int>("logic_int", out var i))
            text = i.ToString();
        else if (data.TryGetValue<SignalState>(DeviceNetworkConstants.LogicState, out var state))
            text = state switch
            {
                SignalState.High => "true",
                SignalState.Low => "false",
                _ => "pulse"
            };
        else
            return;

        ent.Comp.NextChange = now + ent.Comp.ChangeCooldown;
        _appearance.SetData(ent.Owner, TextScreenVisuals.ScreenText, text);
    }
}

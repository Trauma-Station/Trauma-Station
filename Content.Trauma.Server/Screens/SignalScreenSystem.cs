// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.TextScreen;
using Content.Trauma.Common.DeviceLinking;
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
        if (args.Port != ent.Comp.TextPort)
            return;

        TrySetText(ent, "pulse");
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<SignalScreenComponent> ent, ref SignalReceivedEvent<LogicStatePayload> args)
    {
        if (args.Port != ent.Comp.TextPort)
            return;

        TrySetText(ent, args.Data.State switch
        {
            SignalState.High => "true",
            SignalState.Low => "false",
            _ => "pulse"
        });
    }

    [SubscribeLocalEvent]
    private void OnSignalIntReceived(Entity<SignalScreenComponent> ent, ref SignalReceivedEvent<LogicIntPayload> args)
    {
        if (args.Port != ent.Comp.TextPort)
            return;

        TrySetText(ent, args.Data.Value.ToString());
    }

    [SubscribeLocalEvent]
    private void OnSignalStringReceived(Entity<SignalScreenComponent> ent, ref SignalReceivedEvent<LogicStringPayload> args)
    {
        if (args.Port != ent.Comp.TextPort)
            return;

        TrySetText(ent, args.Data.Value);
    }

    private void TrySetText(Entity<SignalScreenComponent> ent, string text)
    {
        var now = _timing.CurTime;
        if (now < ent.Comp.NextChange)
            return;

        ent.Comp.NextChange = now + ent.Comp.ChangeCooldown;
        _appearance.SetData(ent.Owner, TextScreenVisuals.ScreenText, text);
    }
}

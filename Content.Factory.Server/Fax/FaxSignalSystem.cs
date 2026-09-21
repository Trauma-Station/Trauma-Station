// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Fax;
using Content.Shared.Fax.Components;

namespace Content.Factory.Server.Fax;

/// <summary>
/// Handles signals for automated fax machines.
/// </summary>
public sealed partial class FaxSignalSystem : EntitySystem
{
    public static readonly ProtoId<SinkPortPrototype> CopyPort = "FaxCopy";

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<FaxMachineComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port == CopyPort)
            RaiseLocalEvent(ent, new FaxCopyMessage());
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<FaxMachineComponent> ent, ref SignalReceivedEvent<LogicStatePayload> args)
    {
        if (args.Port == CopyPort && args.Data.State != SignalState.Low)
            RaiseLocalEvent(ent, new FaxCopyMessage());
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Construction.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;

namespace Content.Factory.Shared.Construction;

public sealed partial class FlatpackSignalSystem : EntitySystem
{
    public static readonly ProtoId<SinkPortPrototype> OnPort = "On";

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<FlatpackCreatorComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != OnPort)
            return;

        // supercode has no API so we have to do this
        var ev = new FlatpackCreatorStartPackBuiMessage();
        RaiseLocalEvent(ent, ev);
    }
}

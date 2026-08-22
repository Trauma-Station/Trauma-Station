// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceLinking;
using Content.Trauma.Shared.Syndicate.Components;

namespace Content.Trauma.Server.Syndicate;

public sealed partial class SyndicateConverterSignalSystem : EntitySystem
{
    public static readonly ProtoId<SinkPortPrototype> OnPort = "On";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SyndicateConverterComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnSignalReceived(Entity<SyndicateConverterComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != OnPort)
            return;

        // supercode has no API so we have to do this
        var ev = new SyndicateConverterStartPackBuiMessage();
        RaiseLocalEvent(ent, ev);
    }
}

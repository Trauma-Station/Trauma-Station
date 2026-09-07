// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.CartridgeLoader.Cartridges;
using Content.Trauma.Common.NanoChat;
using Content.Shared.Interaction;

namespace Content.Shared.CartridgeLoader.Cartridges;

public sealed partial class LogProbeCartridgeSystem
{
    [SubscribeLocalEvent]
    private void OnRecipientUpdated(ref NanoChatRecipientUpdatedEvent args)
    {
        var query = EntityQueryEnumerator<LogProbeCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var probe, out var cartridge))
        {
            if (probe.ScannedNanoChatData == null || GetEntity(probe.ScannedNanoChatData.Value.Card) != args.CardUid)
                continue;

            if (!TryComp<NanoChatCardComponent>(args.CardUid, out var card))
                continue;

            probe.ScannedNanoChatData = new NanoChatData(
                new Dictionary<uint, NanoChatRecipient>(card.Recipients),
                probe.ScannedNanoChatData.Value.Messages,
                card.Number,
                GetNetEntity(args.CardUid));

            if (cartridge.LoaderUid != null)
                UpdateUiState((uid, probe), cartridge.LoaderUid.Value);
        }
    }

    [SubscribeLocalEvent]
    private void OnMessageReceived(ref NanoChatMessageReceivedEvent args)
    {
        var query = EntityQueryEnumerator<LogProbeCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var probe, out var cartridge))
        {
            if (probe.ScannedNanoChatData is not { } data || GetEntity(data.Card) != args.CardUid)
                continue;

            if (!TryComp<NanoChatCardComponent>(args.CardUid, out var card))
                continue;

            probe.ScannedNanoChatData = new NanoChatData(
                data.Recipients,
                new Dictionary<uint, List<NanoChatMessage>>(card.Messages),
                card.Number,
                data.Card);

            if (cartridge.LoaderUid is { } pda)
                UpdateUiState((uid, probe), pda);
        }
    }

    private void ScanNanoChatCard(Entity<LogProbeCartridgeComponent> ent,
        ref CartridgeRelayedEvent<AfterInteractEvent> args,
        EntityUid target,
        NanoChatCardComponent card)
    {
        var user = args.Args.User;
        _audio.PlayPredicted(ent.Comp.SoundScan,
            target,
            user,
            ent.Comp.SoundScan.Params.WithVariation(0.25f));
        _popup.PopupClient(Loc.GetString("log-probe-scan-nanochat", ("card", target)), target, user);

        ent.Comp.PulledAccessLogs.Clear();

        ent.Comp.ScannedNanoChatData = new NanoChatData(
            new Dictionary<uint, NanoChatRecipient>(card.Recipients),
            new Dictionary<uint, List<NanoChatMessage>>(card.Messages),
            card.Number,
            GetNetEntity(target)
        );
        Dirty(ent);

        UpdateUiState(ent, args.Loader);
    }
}

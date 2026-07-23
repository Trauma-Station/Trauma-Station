// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Access.Components;
using Content.Server.Access.Systems;
using Content.Server.Administration.Logs;
using Content.Server.NameIdentifier;
using Content.Server.Power.Components;
using Content.Server.Radio;
using Content.Shared.CartridgeLoader;
using Content.Shared.Database;
using Content.Shared.Kitchen;
using Content.Shared.NameIdentifier;
using Content.Shared.PDA;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Station;
using Content.Trauma.Common.CartridgeLoader.Cartridges;
using Content.Trauma.Common.NanoChat;
using Content.Trauma.Shared.CartridgeLoader.Cartridges;
using Content.Trauma.Shared.NanoChat;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Trauma.Server.NanoChat;

/// <summary>
///     Handles NanoChat features that are specific to the server.
/// </summary>
public sealed partial class NanoChatSystem : SharedNanoChatSystem
{
    [Dependency] private CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private NameIdentifierSystem _name = default!;
    [Dependency] private NanoChatCartridgeSystem _cartridge = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    // Messages in notifications get cut off after this point
    // no point in storing it on the comp
    private const int NotificationMaxLength = 64;

    private readonly ProtoId<NameIdentifierGroupPrototype> _nameIdentifierGroup = "NanoChat";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NanoChatCardComponent, BeingMicrowavedEvent>(OnMicrowaved, after: [typeof(IdCardSystem)]);
    }

    [SubscribeLocalEvent]
    private void OnInserted(Entity<NanoChatCardComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != PdaComponent.PdaIdSlotId)
            return;

        ent.Comp.PdaUid = args.Container.Owner;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<NanoChatCardComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (args.Container.ID != PdaComponent.PdaIdSlotId)
            return;

        ent.Comp.PdaUid = null;
        Dirty(ent);
    }

    private void OnMicrowaved(Entity<NanoChatCardComponent> ent, ref BeingMicrowavedEvent args)
    {
        // Skip if the entity was deleted (e.g., by ID card system burning it)
        if (TerminatingOrDeleted(ent))
            return;

        var randomPick = _random.NextFloat();

        // Super lucky - erase all messages (10% chance)
        if (randomPick <= 0.10f)
        {
            ent.Comp.Messages.Clear();
            // TODO: these shouldn't be shown at the same time as the popups from IdCardSystem
            // _popup.PopupEntity(Loc.GetString("nanochat-card-microwave-erased", ("card", ent)),
            //     ent,
            //     PopupType.Medium);

            _adminLog.Add(LogType.Action,
                LogImpact.Medium,
                $"{ToPrettyString(args.Microwave)} erased all messages on {ToPrettyString(ent)}");
        }
        else
        {
            // Scramble random messages for random recipients
            ScrambleMessages(ent);
            // _popup.PopupEntity(Loc.GetString("nanochat-card-microwave-scrambled", ("card", ent)),
            //     ent,
            //     PopupType.Medium);

            _adminLog.Add(LogType.Action,
                LogImpact.Medium,
                $"{ToPrettyString(args.Microwave)} scrambled messages on {ToPrettyString(ent)}");
        }

        Dirty(ent);
    }

    private void ScrambleMessages(NanoChatCardComponent component)
    {
        foreach (var (recipientNumber, messages) in component.Messages)
        {
            for (var i = 0; i < messages.Count; i++)
            {
                // 50% chance to scramble each message
                if (!_random.Prob(0.5f))
                    continue;

                var message = messages[i];
                message.Content = ScrambleText(message.Content);
                messages[i] = message;
            }

            // 25% chance to reassign the conversation to a random recipient
            if (_random.Prob(0.25f) && component.Recipients.Count > 0)
            {
                var newRecipient = _random.Pick(component.Recipients.Keys.ToList());
                if (newRecipient == recipientNumber)
                    continue;

                if (!component.Messages.ContainsKey(newRecipient))
                    component.Messages[newRecipient] = new List<NanoChatMessage>();

                component.Messages[newRecipient].AddRange(messages);
                component.Messages[recipientNumber].Clear();
            }
        }
    }

    private string ScrambleText(string text)
    {
        var chars = text.ToCharArray();
        var n = chars.Length;

        // Fisher-Yates shuffle of characters
        while (n > 1)
        {
            n--;
            var k = _random.Next(n + 1);
            (chars[k], chars[n]) = (chars[n], chars[k]);
        }

        return new string(chars);
    }

    [SubscribeLocalEvent]
    private void OnCardInit(Entity<NanoChatCardComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Number != null)
            return;

        // Assign a random number
        _name.GenerateUniqueName(ent, _nameIdentifierGroup, out var number);
        ent.Comp.Number = (uint) number;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnAgentIDSetNumber(Entity<AgentIDCardComponent> ent, ref AgentIDSetNumberMessage args)
    {
        SetNumber(ent.Owner, args.Number);
    }

    public override void TrySendMessage(Entity<NanoChatCartridgeComponent> sender, Entity<NanoChatCardComponent> card, NanoChatMessage message, uint dest, EntityUid user)
    {
        // Attempt delivery
        var (deliveryFailed, recipients) = AttemptMessageDelivery(sender, dest);

        // Update delivery status
        message.DeliveryFailed = deliveryFailed;

        // Store message in sender's outbox under recipient's number
        AddMessage(card.AsNullable(), dest, message);

        // Log message attempt
        var recipientsText = recipients.Count > 0
            ? string.Join(", ", recipients.Select(r => ToPrettyString(r)))
            : $"#{dest:D4}";

        _adminLog.Add(LogType.Chat,
            LogImpact.Low,
            $"{user:user} sent NanoChat message to {recipientsText}: '{message.Content}'{(deliveryFailed ? " [DELIVERY FAILED]" : "")}");

        var ev = new NanoChatMessageReceivedEvent(card);
        RaiseLocalEvent(ref ev);

        if (deliveryFailed)
            return;

        foreach (var recipient in recipients)
        {
            DeliverMessageToRecipient(card, recipient, message);
        }
    }

    /// <summary>
    ///     Attempts to deliver a message to recipients.
    /// </summary>
    /// <param name="sender">The sending cartridge entity</param>
    /// <param name="recipientNumber">The recipient's number</param>
    /// <returns>Tuple containing delivery status and recipients if found.</returns>
    private (bool failed, List<Entity<NanoChatCardComponent>> recipient) AttemptMessageDelivery(
        Entity<NanoChatCartridgeComponent> sender,
        uint recipientNumber)
    {
        var foundRecipients = new List<Entity<NanoChatCardComponent>>();

        // First verify we can send from this device
        var channel = ProtoMan.Index(sender.Comp.RadioChannel);
        var sendAttemptEvent = new RadioSendAttemptEvent(channel, sender);
        RaiseLocalEvent(ref sendAttemptEvent);
        if (sendAttemptEvent.Cancelled)
            return (false, foundRecipients);

        // Find all cards with matching number
        var cardQuery = EntityQueryEnumerator<NanoChatCardComponent>();
        while (cardQuery.MoveNext(out var cardUid, out var card))
        {
            if (card.Number != recipientNumber)
                continue;

            foundRecipients.Add((cardUid, card));
        }

        if (foundRecipients.Count == 0)
            return (false, foundRecipients);

        var senderStation = _station.GetOwningStation(sender);

        // Now check if any of these cards can receive
        var deliverableRecipients = new List<Entity<NanoChatCardComponent>>();
        foreach (var recipient in foundRecipients)
        {
            // Find any cartridges that have this card
            var cartridgeQuery = EntityQueryEnumerator<NanoChatCartridgeComponent, ActiveRadioComponent>();
            while (cartridgeQuery.MoveNext(out var receiverUid, out var receiverCart, out _))
            {
                if (receiverCart.Card != recipient.Owner)
                    continue;

                // Check if devices are on same station/map
                var recipientStation = _station.GetOwningStation(receiverUid);

                // Both entities must be on a station
                if (recipientStation == null || senderStation == null)
                    continue;

                // Must be on same map/station unless long range allowed
                if (!channel.LongRange && recipientStation != senderStation)
                    continue;

                // Needs telecomms
                if (!HasActiveServer(senderStation.Value) || !HasActiveServer(recipientStation.Value))
                    continue;

                // Check if recipient can receive
                var receiveAttemptEv = new RadioReceiveAttemptEvent(channel, sender, receiverUid);
                RaiseLocalEvent(ref receiveAttemptEv);
                if (receiveAttemptEv.Cancelled)
                    continue;

                // Found valid cartridge that can receive
                deliverableRecipients.Add(recipient);
                break; // Only need one valid cartridge per card
            }
        }

        return (deliverableRecipients.Count == 0, deliverableRecipients);
    }

    /// <summary>
    ///     Checks if there are any active telecomms servers on the given station
    /// </summary>
    private bool HasActiveServer(EntityUid station)
    {
        // I have no idea why this isn't public in the RadioSystem
        var query =
            EntityQueryEnumerator<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent>();

        while (query.MoveNext(out var uid, out _, out _, out var power))
        {
            if (_station.GetOwningStation(uid) == station && power.Powered)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Delivers a message to the recipient and handles associated notifications.
    /// </summary>
    /// <param name="sender">The sender's card entity</param>
    /// <param name="recipient">The recipient's card entity</param>
    /// <param name="message">The <see cref="NanoChatMessage" /> to deliver</param>
    private void DeliverMessageToRecipient(Entity<NanoChatCardComponent> sender,
        Entity<NanoChatCardComponent> recipient,
        NanoChatMessage message)
    {
        var senderNumber = sender.Comp.Number;
        if (senderNumber == null)
            return;

        // Always try to get and add sender info to recipient's contacts
        if (!EnsureRecipientExists(recipient, senderNumber.Value))
            return;

        AddMessage((recipient, recipient.Comp), senderNumber.Value, message with { DeliveryFailed = false });

        if (recipient.Comp.IsClosed || GetCurrentChat((recipient, recipient.Comp)) != senderNumber)
            HandleUnreadNotification(recipient, message, (uint) senderNumber);

        var msgEv = new NanoChatMessageReceivedEvent(recipient);
        RaiseLocalEvent(ref msgEv);
        _cartridge.UpdateUIForCard(recipient);
    }

    /// <summary>
    ///     Handles unread message notifications and updates unread status.
    /// </summary>
    private void HandleUnreadNotification(Entity<NanoChatCardComponent> recipient,
        NanoChatMessage message,
        uint senderNumber)
    {
        // Get sender name from contacts or fall back to number
        var recipients = GetRecipients((recipient, recipient.Comp));
        var senderName = recipients.TryGetValue(message.SenderId, out var senderRecipient)
            ? senderRecipient.Name
            : $"#{message.SenderId:D4}";
        var hasSelectedCurrentChat = GetCurrentChat(recipient.AsNullable()) == senderNumber;

        // Update unread status
        if (!hasSelectedCurrentChat)
            SetRecipient((recipient, recipient.Comp),
                message.SenderId,
                senderRecipient with { HasUnread = true });

        if (recipient.Comp.NotificationsMuted ||
            recipient.Comp.PdaUid is not { } pdaUid ||
            !TryComp<CartridgeLoaderComponent>(pdaUid, out var loader) ||
            // Don't notify if the recipient has the NanoChat program open with this chat selected.
            (hasSelectedCurrentChat &&
                _ui.IsUiOpen(pdaUid, PdaUiKey.Key) &&
                HasComp<NanoChatCartridgeComponent>(loader.ActiveProgram)))
        {
            return;
        }

        _cartridgeLoader.SendNotification(pdaUid,
            Loc.GetString("nano-chat-new-message-title", ("sender", senderName)),
            Loc.GetString("nano-chat-new-message-body", ("message", TruncateMessage(message.Content))),
            loader);
    }

    /// <summary>
    ///     Truncates a message to the notification maximum length.
    /// </summary>
    private static string TruncateMessage(string message)
        => message.Length <= NotificationMaxLength
            ? message
            : message[..(NotificationMaxLength - 4)] + " [...]";
}

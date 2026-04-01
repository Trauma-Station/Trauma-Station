// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Cryptography;
using Content.Trauma.Common.CartridgeLoader.Cartridges;

namespace Content.Trauma.Common.NanoChat;

public abstract partial class CommonNanoChatSystem : EntitySystem
{
    /// <summary>
    ///     Gets the NanoChat number for a card.
    /// </summary>
    public abstract uint? GetNumber(Entity<NanoChatCardComponent?> card);

    /// <summary>
    ///     Sets the NanoChat number for a card.
    /// </summary>
    public abstract void SetNumber(Entity<NanoChatCardComponent?> card, uint number);

    /// <summary>
    ///     Gets the recipients dictionary from a card.
    /// </summary>
    public abstract IReadOnlyDictionary<uint, NanoChatRecipient> GetRecipients(Entity<NanoChatCardComponent?> card);

    /// <summary>
    ///     Sets a specific recipient in the card.
    /// </summary>
    public abstract void SetRecipient(Entity<NanoChatCardComponent?> card, uint number, NanoChatRecipient recipient);

    /// <summary>
    ///     Gets all messages for a specific recipient.
    /// </summary>
    public abstract List<NanoChatMessage>? GetMessagesForRecipient(Entity<NanoChatCardComponent?> card, uint recipientNumber);

    /// <summary>
    ///     Adds a message to a recipient's conversation.
    /// </summary>
    public abstract void AddMessage(Entity<NanoChatCardComponent?> card, uint recipientNumber, NanoChatMessage message);

    /// <summary>
    ///     Clears all messages and recipients from the card.
    /// </summary>
    public abstract void Clear(Entity<NanoChatCardComponent?> card);
}

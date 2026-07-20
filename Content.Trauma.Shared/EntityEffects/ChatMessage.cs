// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat;
using Content.Shared.EntityEffects;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Sends the target entity a chat message only seen by them.
/// </summary>
public sealed partial class ChatMessage : EntityEffectBase<ChatMessage>
{
    [DataField(required: true)]
    public ChatChannel Channel = ChatChannel.None;

    /// <summary>
    /// The message text.
    /// </summary>
    [DataField(required: true)]
    public string Message = string.Empty;

    /// <summary>
    /// The wrapped message including extra markup tags.
    /// Falls back to <see cref="Message"/> if null.
    /// </summary>
    [DataField]
    public string? WrappedMessage;

    [DataField]
    public bool HideChat = false;

    [DataField]
    public Color? Color;
}

public sealed partial class ChatMessageEffectSystem : EntityEffectSystem<ActorComponent, ChatMessage>
{
    [Dependency] private ISharedChatManager _chat = default!;

    protected override void Effect(Entity<ActorComponent> ent, ref EntityEffectEvent<ChatMessage> args)
    {
        var e = args.Effect;
        var wrapped = e.WrappedMessage ?? e.Message;
        var source = args.User ?? ent.Owner;
        var channel = ent.Comp.PlayerSession.Channel;
        _chat.ChatMessageToOne(e.Channel, e.Message, wrapped, source, e.HideChat, channel, e.Color);
    }
}

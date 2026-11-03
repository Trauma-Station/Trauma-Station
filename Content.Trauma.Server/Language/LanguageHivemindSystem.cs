// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Trauma.Common.Language;
using Content.Trauma.Shared.Language;
using Content.Trauma.Shared.Language.Systems;
using Robust.Shared.Player;
using System.Linq;

namespace Content.Trauma.Server.Language;

public sealed partial class LanguageHivemindSystem : EntitySystem
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private SharedLanguageSystem _language = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LanguageHivemindComponent, EntitySpokeEvent>(OnEntitySpoke);
    }

    private void OnEntitySpoke(Entity<LanguageHivemindComponent> ent, ref EntitySpokeEvent args)
    {
        if (args.Source != ent.Owner || args.Language.ID != ent.Comp.Language || args.IsWhisper)
            return;

        SendMessage(ent, args.Message, false, args.Language);
    }

    private void SendMessage(Entity<LanguageHivemindComponent> ent, string message, bool hideChat, LanguagePrototype language)
    {
        var clients = GetRecipients(language.ID);
        var playerName = Name(ent);
        var wrappedMessage = Loc.GetString(
            // formatting is fine for non xenos too
            "chat-manager-send-xeno-hivemind-chat-wrap-message",
            ("channelName", Loc.GetString(ent.Comp.ChannelName)),
            ("player", playerName),
            ("message", FormattedMessage.EscapeText(message)));

        _chat.ChatMessageToMany(
            ChatChannel.CollectiveMind,
            message,
            wrappedMessage,
            ent,
            hideChat,
            true,
            clients,
            language.SpeechOverride.Color);
    }

    private List<INetChannel> GetRecipients(ProtoId<LanguagePrototype> languageId)
        => Filter.Empty()
            .AddWhereAttachedEntity(entity => _language.CanUnderstand(entity, languageId))
            .Recipients
            .Union(_admin.ActiveAdmins)
            .Select(p => p.Channel)
            .ToList();
}

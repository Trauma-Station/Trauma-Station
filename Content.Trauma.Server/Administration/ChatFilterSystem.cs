// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Common.Chat;
using Content.Trauma.Shared.Administration;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using System.Text.RegularExpressions;

namespace Content.Trauma.Server.Administration;

public sealed partial class ChatFilterSystem : EntitySystem
{
    [Dependency] private IBanManager _ban = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private EntityQuery<ActorComponent> _actorQuery = default!;

    private List<(Regex, ChatFilterPrototype)> _filters = new();

    public override void Initialize()
    {
        base.Initialize();

        LoadPrototypes();
    }

    [SubscribeLocalEvent]
    private void OnPlayerMessageAttempt(ref PlayerMessageAttemptEvent args)
    {
        args.Cancelled |= CheckMessage(args.Session, args.Message);
    }

    [SubscribeLocalEvent]
    private void OnUserMessageAttempt(ref UserMessageAttemptEvent args)
    {
        args.Cancelled |= CheckMessage(args.User, args.Message);
    }

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ChatFilterPrototype>())
            LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        // unsub incase a prototype was removed so the cvar isnt used anymore, also simplifies sub logic
        foreach (var (_, proto) in _filters)
        {
            if (proto.Cvar is { } cvar)
                _cfg.UnsubValueChanged<string>(cvar, OnValueChanged);
        }

        _filters.Clear();
        foreach (var proto in ProtoMan.EnumeratePrototypes<ChatFilterPrototype>())
        {
            var text = proto.Regex;
            if (proto.Cvar is { } cvar)
            {
                _cfg.OnValueChanged<string>(cvar, OnValueChanged);
                text = _cfg.GetCVar<string>(cvar);
            }

            if (ParseRegex(text, proto.ID) is { } regex)
                _filters.Add((regex, proto));
        }
    }

    private void OnValueChanged(string regex)
    {
        // identifying the specific filter is too much effort, its fine to just reload them all
        LoadPrototypes();
    }

    private Regex? ParseRegex(string text, string name)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        var timeout = TimeSpan.FromMilliseconds(1); // incase config is stupid
        try
        {
            return new Regex(text, RegexOptions.Compiled | RegexOptions.IgnoreCase, timeout);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to parse {name} filter regex: {e}");
            return null;
        }
    }

    public bool CheckMessage(EntityUid user, string message)
        => _actorQuery.TryComp(user, out var actor) && CheckMessage(actor.PlayerSession, message);

    /// <summary>
    /// Checks if a message contains gamer words, returning true if it can't be sent.
    /// Handles punishment if there are any.
    /// </summary>
    public bool CheckMessage(ICommonSession player, string message)
    {
        var blocked = false;
        foreach (var (regex, proto) in _filters)
        {
            if (!regex.IsMatch(message))
                continue;

            Punish(player, message, proto);
            blocked |= proto.Block;
        }

        return blocked;
    }

    // Starring
    //    Punished "Venom" Snake
    public void Punish(ICommonSession player, string message, ChatFilterPrototype proto)
    {
        // 1. tell admins
        _chat.SendAdminAlert($"Player {player.Name} has hit the chat filter {proto.ID}");
        // 2. smite by god so the people know
        if (player.AttachedEntity is {} mob && proto.Effects is { } effects)
            _effects.ApplyEffects(mob, effects, user: mob, predicted: false);
        // 3. automatic permaban if wanted
        if (proto.BanMessage is not { } preamble)
            return;

        var reason = $"{preamble}\nOffending message: {message}";
        var ban = new CreateServerBanInfo(reason);
        ban.WithWebhookReason(preamble) // dont show everyone the message...
            .AddUser(player.UserId, player.Name)
            .AddAddress(player.Channel.RemoteEndPoint.Address)
            .WithSeverity(NoteSeverity.High);
        _ban.CreateServerBan(ban);
    }
}

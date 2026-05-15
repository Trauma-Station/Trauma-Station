// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Smites;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Shared.Database;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Common.Chat;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using System.Text.RegularExpressions;

namespace Content.Trauma.Server.Administration;

public sealed partial class GamerWordsSystem : EntitySystem
{
    [Dependency] private IBanManager _ban = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ThunderstrikeSystem _thunderstrike = default!;
    [Dependency] private EntityQuery<ActorComponent> _actorQuery = default!;

    private Regex? _regex;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerMessageAttemptEvent>(OnPlayerMessageAttempt);
        SubscribeLocalEvent<UserMessageAttemptEvent>(OnUserMessageAttempt);

        Subs.CVar(_cfg, TraumaCVars.GamerWordsRegex, SetRegex, true);
    }

    private void OnPlayerMessageAttempt(ref PlayerMessageAttemptEvent args)
    {
        args.Cancelled |= CheckMessage(args.Session, args.Message);
    }

    private void OnUserMessageAttempt(ref UserMessageAttemptEvent args)
    {
        args.Cancelled |= CheckMessage(args.User, args.Message);
    }

    private void SetRegex(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            _regex = null;
            return;
        }

        var timeout = TimeSpan.FromMilliseconds(1); // incase config is stupid
        try
        {
            _regex = new(text, RegexOptions.Compiled | RegexOptions.IgnoreCase, timeout);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to parse gamer_words_regex: {e}");
            _regex = null;
        }
    }

    public bool CheckMessage(EntityUid user, string message)
        => _actorQuery.TryComp(user, out var actor) && CheckMessage(actor.PlayerSession, message);

    /// <summary>
    /// Checks if a message contains gamer words, returning false if there are none.
    /// Handles it if there are any.
    /// </summary>
    public bool CheckMessage(ICommonSession player, string message)
    {
        if (!IsEvil(message))
            return false;

        // 1. tell admins
        _chat.SendAdminAlert("Player {player.Name} has been smitten for trying to say gamer words");
        // 2. smite by god so the people know
        if (player.AttachedEntity is {} mob)
            _thunderstrike.Smite(mob);
        // 3. automatic permaban
        var reason = $"Automatically banned for violation of rule C9: No bigotry.\nOffending message: {message}";
        var ban = new CreateServerBanInfo(reason);
        ban.AddUser(player.UserId, player.Name)
            .AddAddress(player.Channel.RemoteEndPoint.Address)
            .WithSeverity(NoteSeverity.High);
        _ban.CreateServerBan(ban);
        return true;
    }

    public bool IsEvil(string message)
        => _regex?.IsMatch(message) == true;
}

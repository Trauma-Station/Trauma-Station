// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.Players.RateLimiting;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Players.RateLimiting;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Common.Mentor;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Trauma.Server.Mentor;

public sealed partial class MentorManager : IPostInjectInit
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private ILogManager _log = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private PlayerRateLimitManager _rate = default!;
    [Dependency] private UserDbDataManager _db = default!;

    private const string RateLimitKey = "MentorHelp";

    private ISawmill _sawmill = default!;

    private readonly List<ICommonSession> _activeMentors = new();
    private readonly Dictionary<NetUserId, bool> _mentors = new();

    private void FinishLoad(ICommonSession player)
    {
        SendMentorStatus(player);
    }

    private void ClientDisconnected(ICommonSession player)
    {
        _mentors.Remove(player.UserId);
        _activeMentors.Remove(player);
    }

    private void OnMentorSendMessage(MentorSendMessageMsg message)
    {
        var destination = new NetUserId(message.To);
        if (!_player.TryGetSessionById(destination, out var destinationSession))
            return;

        var author = message.MsgChannel.UserId;
        if (!_player.TryGetSessionById(author, out var authorSession) ||
            !_activeMentors.Contains(authorSession))
        {
            return;
        }

        SendMentorMessage(
            destination,
            destinationSession.Name,
            author,
            authorSession.Name,
            message.Message,
            destinationSession.Channel
        );
    }

    private void OnMentorHelpMessage(MentorHelpMsg message)
    {
        if (!_player.TryGetSessionById(message.MsgChannel.UserId, out var author))
            return;

        var mind = _entity.System<SharedMindSystem>();
        SendMentorMessage(author.UserId, author.Name, author.UserId, mind.GetCharacterName(author.UserId) ?? author.Name, message.Message, message.MsgChannel);
    }

    private void OnAdminPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (_admin.HasAdminFlag(args.Player, AdminFlags.MentorHelp) && !_activeMentors.Contains(args.Player))
        {
            _activeMentors.Add(args.Player);
            SendMentorStatus(args.Player);
        }

        if (!_admin.HasAdminFlag(args.Player, AdminFlags.MentorHelp) && _activeMentors.Contains(args.Player))
        {
            _activeMentors.Remove(args.Player);
            SendMentorStatus(args.Player);
        }
    }

    private void SendMentorStatus(ICommonSession player)
    {
        var isMentor = _activeMentors.Contains(player);
        var canReMentor = _mentors.TryGetValue(player.UserId, out var mentor) && mentor;
        var msg = new MentorStatusMsg()
        {
            IsMentor = isMentor,
            CanReMentor = canReMentor,
        };

        _net.ServerSendMessage(msg, player.Channel);
    }

    private void SendMentorMessage(NetUserId destination, string destinationName, NetUserId author, string authorName, string message, INetChannel destinationChannel)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var recipients = new HashSet<INetChannel> { destinationChannel };
        var isMentor = false;
        foreach (var active in _activeMentors)
        {
            if (active.UserId == author)
                isMentor = true;

            recipients.Add(active.Channel);
        }

        var mentorMsg = new MentorMessage(
            destination,
            destinationName,
            author,
            authorName,
            message,
            DateTime.Now,
            isMentor
        );
        var messages = new List<MentorMessage> { mentorMsg };
        var receive = new MentorMessagesReceivedMsg { Messages = messages };
        foreach (var recipient in recipients)
        {
            try
            {
                _net.ServerSendMessage(receive, recipient);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Error sending mentor help message:\n{e}");
            }
        }
    }

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<MentorStatusMsg>();
        _net.RegisterNetMessage<MentorSendMessageMsg>(OnMentorSendMessage);
        _net.RegisterNetMessage<MentorHelpMsg>(OnMentorHelpMessage);
        _net.RegisterNetMessage<MentorMessagesReceivedMsg>();

        _sawmill = _log.GetSawmill("mhelp");

        _db.AddOnFinishLoad(FinishLoad);
        _db.AddOnPlayerDisconnect(ClientDisconnected);

        if (_config.IsCVarRegistered(TraumaCVars.MentorHelpRateLimitPeriod.Name) &&
            _config.IsCVarRegistered(TraumaCVars.MentorHelpRateLimitCount.Name))
        {
            _rate.Register(
                RateLimitKey,
                new RateLimitRegistration(
                    TraumaCVars.MentorHelpRateLimitPeriod,
                    TraumaCVars.MentorHelpRateLimitCount,
                    _ => { }
                )
            );
        }

        _admin.OnPermsChanged += OnAdminPermsChanged;
    }
}

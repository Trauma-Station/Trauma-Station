using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Network;

namespace Content.Server.Database;

public sealed partial class ServerDbManager
{
    #region Patrons

    public Task<Guid?> GetLinkingCode(Guid player)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetLinkingCode(player));
    }

    public Task SetLinkingCode(Guid player, Guid code)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SetLinkingCode(player, code));
    }

    public Task<bool> HasLinkedAccount(Guid player, CancellationToken cancel)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.HasLinkedAccount(player, cancel));
    }

    public Task<RMCPatron?> GetPatron(Guid player, CancellationToken cancel)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPatron(player, cancel));
    }

    public Task<List<RMCPatron>> GetAllPatrons()
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetAllPatrons());
    }

    public Task SetGhostColor(Guid player, System.Drawing.Color? color)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SetGhostColor(player, color));
    }

    public Task SetLobbyMessage(Guid player, string message)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SetLobbyMessage(player, message));
    }

    public Task SetNTShoutout(Guid player, string name)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SetNTShoutout(player, name));
    }

    public Task<List<(string, string)>> GetLobbyMessages()
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetLobbyMessages());
    }

    public Task<List<string>> GetShoutouts()
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetShoutouts());
    }

    #endregion

    #region Currency

    public Task<int> GetServerCurrency(NetUserId userId)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetServerCurrency(userId));
    }

    public Task SetServerCurrency(NetUserId userId, int currency)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SetServerCurrency(userId, currency));
    }

    public Task<int> ModifyServerCurrency(NetUserId userId, int currencyDelta)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.ModifyServerCurrency(userId, currencyDelta));
    }

    public Task WipeServerCurrency()
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.WipeServerCurrency());
    }

    #endregion

    #region Polls

    public Task<int> CreatePollAsync(Poll poll)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.CreatePollAsync(poll));
    }

    public Task<Poll?> GetPollAsync(int pollId, CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPollAsync(pollId, cancel));
    }

    public Task<List<Poll>> GetActivePollsAsync(CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetActivePollsAsync(cancel));
    }

    public Task<List<Poll>> GetAllPollsAsync(bool includeInactive = true, CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetAllPollsAsync(includeInactive, cancel));
    }

    public Task UpdatePollStatusAsync(int pollId, bool active, CancellationToken cancel = default)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.UpdatePollStatusAsync(pollId, active, cancel));
    }

    public Task<bool> AddPollVoteAsync(int pollId, int optionId, NetUserId userId, CancellationToken cancel = default)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.AddPollVoteAsync(pollId, optionId, userId, cancel));
    }

    public Task<bool> RemovePollVoteAsync(int pollId, int optionId, NetUserId userId, CancellationToken cancel = default)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.RemovePollVoteAsync(pollId, optionId, userId, cancel));
    }

    public Task<List<PollVote>> GetPollVotesAsync(int pollId, CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPollVotesAsync(pollId, cancel));
    }

    public Task<List<PollVote>> GetPlayerVotesAsync(int pollId, NetUserId userId, CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPlayerVotesAsync(pollId, userId, cancel));
    }

    public Task<bool> HasPlayerVotedAsync(int pollId, NetUserId userId, CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.HasPlayerVotedAsync(pollId, userId, cancel));
    }

    public Task<Dictionary<int, int>> GetPollResultsAsync(int pollId, CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPollResultsAsync(pollId, cancel));
    }

    #endregion
}

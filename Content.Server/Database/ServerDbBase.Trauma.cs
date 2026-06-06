using Robust.Shared.Network;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    #region Patrons

    public async Task<Guid?> GetLinkingCode(Guid player)
    {
        await using var db = await GetDb();
        var linking = await db.DbContext.RMCLinkingCodes.FirstOrDefaultAsync(l => l.PlayerId == player);
        return linking?.Code;
    }

    public async Task SetLinkingCode(Guid player, Guid code)
    {
        await using var db = await GetDb();
        var linking = await db.DbContext.RMCLinkingCodes.FirstOrDefaultAsync(l => l.PlayerId == player);
        if (linking == null)
        {
            linking = new RMCLinkingCodes { PlayerId = player };
            db.DbContext.RMCLinkingCodes.Add(linking);
        }

        linking.Code = code;
        linking.CreationTime = DateTime.UtcNow;
        await db.DbContext.SaveChangesAsync();
    }

    public async Task<bool> HasLinkedAccount(Guid player, CancellationToken cancel)
    {
        await using var db = await GetDb(cancel);
        return await db.DbContext.RMCLinkedAccounts.AnyAsync(l => l.PlayerId == player, cancel);
    }

    public async Task<RMCPatron?> GetPatron(Guid player, CancellationToken cancel)
    {
        await using var db = await GetDb(cancel);
        var patron = await db.DbContext.RMCPatrons
            .Include(p => p.Tier)
            .Include(p => p.LobbyMessage)
            .Include(p => p.RoundEndNTShoutout)
            .FirstOrDefaultAsync(p => p.PlayerId == player, cancellationToken: cancel);
        return patron;
    }

    public async Task<List<RMCPatron>> GetAllPatrons()
    {
        await using var db = await GetDb();
        return await db.DbContext.RMCPatrons
            .Include(p => p.Player)
            .Include(p => p.Tier)
            .ToListAsync();
    }

    public async Task SetGhostColor(Guid player, System.Drawing.Color? color)
    {
        await using var db = await GetDb();
        var patron = await db.DbContext.RMCPatrons.FirstOrDefaultAsync(p => p.PlayerId == player);
        if (patron == null)
            return;

        patron.GhostColor = color?.ToArgb();
        await db.DbContext.SaveChangesAsync();
    }

    public async Task SetLobbyMessage(Guid player, string message)
    {
        await using var db = await GetDb();
        var msg = await db.DbContext.RMCPatronLobbyMessages
            .Include(l => l.Patron)
            .FirstOrDefaultAsync(p => p.PatronId == player);
        msg ??= db.DbContext.RMCPatronLobbyMessages
            .Add(new RMCPatronLobbyMessage
            {
                PatronId = player,
                Message = message,
            })
            .Entity;
        msg.Message = message;

        await db.DbContext.SaveChangesAsync();
    }

    public async Task SetNTShoutout(Guid player, string name)
    {
        await using var db = await GetDb();
        var msg = await db.DbContext.RMCPatronRoundEndNTShoutouts
            .Include(s => s.Patron)
            .FirstOrDefaultAsync(p => p.PatronId == player);
        msg ??= db.DbContext.RMCPatronRoundEndNTShoutouts
            .Add(new RMCPatronRoundEndNTShoutout()
            {
                PatronId = player,
                Name = name,
            })
            .Entity;
        msg.Name = name;

        await db.DbContext.SaveChangesAsync();
    }

    public async Task<List<(string Message, string User)>> GetLobbyMessages()
    {
        await using var db = await GetDb();
        var messages = await db.DbContext.RMCPatronLobbyMessages
            .Include(p => p.Patron)
            .ThenInclude(p => p.Player)
            .Where(p => p.Patron.Tier.LobbyMessage)
            .Where(p => !string.IsNullOrWhiteSpace(p.Message))
            .Select(p => new { p.Message, p.Patron.Player.LastSeenUserName })
            .Select(p => new ValueTuple<string, string>(p.Message, p.LastSeenUserName))
            .ToListAsync();

        return messages;
    }

    public async Task<List<string>> GetShoutouts()
    {
        await using var db = await GetDb();
        var ntNames = await db.DbContext.RMCPatronRoundEndNTShoutouts
            .Include(p => p.Patron)
            .Where(p => p.Patron.Tier.RoundEndShoutout)
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => p.Name)
            .ToListAsync();

        return ntNames;
    }

    #endregion

    #region Currency

    public async Task<int> GetServerCurrency(NetUserId userId)
    {
        await using var db = await GetDb();

        return await db.DbContext.Player
            .Where(dbPlayer => dbPlayer.UserId == userId)
            .Select(dbPlayer => dbPlayer.ServerCurrency)
            .SingleOrDefaultAsync();
    }

    public async Task SetServerCurrency(NetUserId userId, int currency)
    {
        await using var db = await GetDb();

        var dbPlayer = await db.DbContext.Player.Where(dbPlayer => dbPlayer.UserId == userId).SingleOrDefaultAsync();
        if (dbPlayer == null)
            return;

        dbPlayer.ServerCurrency = currency;
        await db.DbContext.SaveChangesAsync();
    }

    public async Task<int> ModifyServerCurrency(NetUserId userId, int currencyDelta)
    {
        await using var db = await GetDb();

        var dbPlayer = await db.DbContext.Player.Where(dbPlayer => dbPlayer.UserId == userId).SingleOrDefaultAsync();
        if (dbPlayer == null)
            return currencyDelta;

        dbPlayer.ServerCurrency += currencyDelta;
        await db.DbContext.SaveChangesAsync();
        return dbPlayer.ServerCurrency;
    }

    public async Task WipeServerCurrency()
    {
        await using var db = await GetDb();

        foreach (var player in db.DbContext.Player.Where(player => player.ServerCurrency != 0))
        {
            player.ServerCurrency = 0;
        }
        await db.DbContext.SaveChangesAsync();
    }

    #endregion

    #region Polls

    public async Task<int> CreatePollAsync(Poll poll)
    {
        await using var db = await GetDb();
        db.DbContext.Polls.Add(poll);
        await db.DbContext.SaveChangesAsync();
        return poll.Id;
    }

    public async Task<Poll?> GetPollAsync(int pollId, CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);

        return await db.DbContext.Polls
            .Include(p => p.Options)
            .Include(p => p.Votes)
            .Include(p => p.CreatedBy)
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Id == pollId, cancel);
    }

    public async Task<List<Poll>> GetActivePollsAsync(CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);

        return await db.DbContext.Polls
            .Include(p => p.Options)
            .Include(p => p.CreatedBy)
            .AsSplitQuery()
            .Where(p => p.Active && (p.EndTime == null || p.EndTime > DateTime.UtcNow))
            .OrderByDescending(p => p.StartTime)
            .ToListAsync(cancel);
    }

    public async Task<List<Poll>> GetAllPollsAsync(bool includeInactive = true, CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);

        var query = db.DbContext.Polls
            .Include(p => p.Options)
            .Include(p => p.CreatedBy)
            .AsSplitQuery();

        if (!includeInactive)
            query = query.Where(p => p.Active);

        return await query.OrderByDescending(p => p.StartTime).ToListAsync(cancel);
    }

    public async Task UpdatePollStatusAsync(int pollId, bool active, CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);

        var poll = await db.DbContext.Polls.SingleOrDefaultAsync(p => p.Id == pollId, cancel);
        if (poll == null)
            return;

        poll.Active = active;
        await db.DbContext.SaveChangesAsync(cancel);
    }

    public async Task<bool> AddPollVoteAsync(int pollId, int optionId, NetUserId userId, CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);

        var poll = await db.DbContext.Polls
            .Include(p => p.Options)
            .SingleOrDefaultAsync(p => p.Id == pollId, cancel);

        if (poll?.Active != true)
            return false;

        if (poll.EndTime < DateTime.UtcNow)
            return false;

        if (!poll.Options.Any(o => o.Id == optionId))
            return false;

        var existingVote = await db.DbContext.PollVotes
            .AnyAsync(v => v.PollId == pollId && v.PollOptionId == optionId && v.PlayerUserId == userId.UserId, cancel);

        if (existingVote)
            return false;

        if (!poll.AllowMultipleChoices)
        {
            var existingVotes = await db.DbContext.PollVotes
                .Where(v => v.PollId == pollId && v.PlayerUserId == userId.UserId)
                .ToListAsync(cancel);

            db.DbContext.PollVotes.RemoveRange(existingVotes);
        }

        var vote = new PollVote
        {
            PollId = pollId,
            PollOptionId = optionId,
            PlayerUserId = userId.UserId,
            VotedAt = DateTime.UtcNow
        };

        db.DbContext.PollVotes.Add(vote);
        await db.DbContext.SaveChangesAsync(cancel);
        return true;
    }

    public async Task<bool> RemovePollVoteAsync(int pollId, int optionId, NetUserId userId, CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);

        var vote = await db.DbContext.PollVotes
            .FirstOrDefaultAsync(v => v.PollId == pollId && v.PollOptionId == optionId && v.PlayerUserId == userId.UserId, cancel);

        if (vote == null)
            return false;

        db.DbContext.PollVotes.Remove(vote);
        await db.DbContext.SaveChangesAsync(cancel);
        return true;
    }

    public async Task<List<PollVote>> GetPollVotesAsync(int pollId, CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);

        return await db.DbContext.PollVotes
            .Include(v => v.Player)
            .Include(v => v.PollOption)
            .Where(v => v.PollId == pollId)
            .ToListAsync(cancel);
    }

    public async Task<List<PollVote>> GetPlayerVotesAsync(int pollId, NetUserId userId, CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);

        return await db.DbContext.PollVotes
            .Include(v => v.PollOption)
            .Where(v => v.PollId == pollId && v.PlayerUserId == userId.UserId)
            .ToListAsync(cancel);
    }

    public async Task<bool> HasPlayerVotedAsync(int pollId, NetUserId userId, CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);

        return await db.DbContext.PollVotes
            .AnyAsync(v => v.PollId == pollId && v.PlayerUserId == userId.UserId, cancel);
    }

    public async Task<Dictionary<int, int>> GetPollResultsAsync(int pollId, CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);

        return await db.DbContext.PollVotes
            .Where(v => v.PollId == pollId)
            .GroupBy(v => v.PollOptionId)
            .Select(g => new { OptionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OptionId, x => x.Count, cancel);
    }

    #endregion
}

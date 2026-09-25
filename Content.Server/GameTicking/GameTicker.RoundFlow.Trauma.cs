
namespace Content.Server.GameTicking;
public sealed partial class GameTicker
{
    /// <summary>
    /// Returns the readied player count as well as half a player per unreadied player.
    /// </summary>
    public int ReadyPlayerCountEffective()
    {
        int total = ReadyPlayerCount();
        return total + (_playerGameStatuses.Count - total) / 2;
    }
}

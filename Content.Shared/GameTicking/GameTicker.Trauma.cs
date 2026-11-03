namespace Content.Shared.GameTicking;

public abstract partial class GameTicker
{
    /// <summary>
    /// Returns the readied player count as well as half a player per unreadied player.
    /// </summary>
    public virtual int ReadyPlayerCountEffective()
        => 0;
}

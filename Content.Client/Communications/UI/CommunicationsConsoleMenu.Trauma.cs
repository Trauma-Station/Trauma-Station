using System.Globalization;

namespace Content.Client.Communications.UI;

/// <summary>
/// Trauma - alert level lock related UI stuff
/// </summary>
public sealed partial class CommunicationsConsoleMenu
{
    public string LockedLevel = string.Empty;
    public TimeSpan? NextUnlock;

    /// <summary>
    /// Updates the unlock text when waiting for a locked alert level.
    /// </summary>
    public void UpdateUnlock()
    {
        if (NextUnlock is not {} unlock)
        {
            UnlockContainer.Visible = false;
            return;
        }

        var level = Loc.GetString($"alert-level-{LockedLevel}");
        var now = _timing.CurTime;
        UnlockLabel.Text = unlock > now
            ? Loc.GetString("comms-console-menu-level-unlocked-at",
                ("time", (unlock - now).ToString(@"hh\:mm\:ss", CultureInfo.CurrentCulture)),
                ("level", level))
            : Loc.GetString("comms-console-menu-level-unlocked", ("level", level));
        UnlockContainer.Visible = true;
    }
}

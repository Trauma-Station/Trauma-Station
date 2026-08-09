// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.AlertLevel;
using Content.Trauma.Common.AlertLevel;
using System.Globalization;

namespace Content.Client.Communications.UI;

/// <summary>
/// Trauma - alert level lock related UI stuff
/// </summary>
public sealed partial class CommunicationsConsoleMenu
{
    [Dependency] private IEntityManager _ent = default!;
    public AlertLevelSystem AlertLevel = default!;

    public EntityUid Station;
    private int? _unlockSeconds = -1;

    /// <summary>
    /// Updates the unlock text when waiting for a locked alert level.
    /// </summary>
    public void UpdateUnlock()
    {
        if (!Station.Valid)
            return;

        // TODO: have this use after handle state event + ui injection
        var ev = new CheckAlertLevelLockEvent();
        _ent.EventBus.RaiseLocalEvent(Station, ref ev);
        if (ev.NextUnlock is not {} unlock)
        {
            UnlockContainer.Visible = false;
            return;
        }

        UnlockContainer.Visible = true;

        var now = _timing.CurTime;
        TimeSpan? remaining = unlock > now ? unlock - now : null;
        int? seconds = remaining?.TotalSeconds is { } secs ? (int) secs : null;
        var locked = seconds != null;
        var wasLocked = _unlockSeconds != null;
        if (seconds == _unlockSeconds)
            return;

        _unlockSeconds = seconds;

        if (locked != wasLocked)
        {
            SelectableAlertLevels = AlertLevel.GetSelectableAlertLevels(Station);
            AlertLevelSelectable = AlertLevel.CanChangeAlertLevel(Station);
        }

        var level = _protoMan.Index<AlertLevelPrototype>(ev.LockedLevel).LocalizedName;
        UnlockLabel.Text = remaining != null
            ? Loc.GetString("comms-console-menu-level-unlocked-at",
                ("time", remaining.Value.ToString(@"hh\:mm\:ss", CultureInfo.CurrentCulture)),
                ("level", level))
            : Loc.GetString("comms-console-menu-level-unlocked", ("level", level));
    }
}

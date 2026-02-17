// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.AlertLevel;

/// <summary>
/// Raised on a station entity to cancel changing alert level.
/// Not raised if SetLevel is called with <c>force: true</c>
/// </summary>
[ByRefEvent]
public record struct ChangeAlertLevelAttemptEvent(string AlertLevel, bool Cancelled = false)
{
    public void Cancel()
    {
        Cancelled = true;
    }
}

/// <summary>
/// Raised on a station entity to check if an alert level is currently locked, and when it will be unlocked.
/// </summary>
[ByRefEvent]
public record struct CheckAlertLevelLockEvent(string LockedLevel = "", TimeSpan? NextUnlock = null);

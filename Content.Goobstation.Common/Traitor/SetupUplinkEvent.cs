// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.Traitor;

/// <summary>
/// Raised after uplink was set
/// </summary>
[ByRefEvent]
public record struct SetupUplinkEvent
{
    public EntityUid User;
    public string? BriefingEntry;
    // This one is for the character window
    public string? BriefingEntryShort;

    public bool Handled;
}

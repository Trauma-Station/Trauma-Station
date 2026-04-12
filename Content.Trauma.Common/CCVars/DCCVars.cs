// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Trauma.Common.CCVars;

/// <summary>
/// DeltaV specific cvars.
/// </summary>
[CVarDefs]
// ReSharper disable once InconsistentNaming - Shush you
public sealed class DCCVars
{
    /// <summary>
    /// Disables all vision filters for species like Vulpkanin or Harpies. There are good reasons someone might want to disable these.
    /// </summary>
    public static readonly CVarDef<bool> NoVisionFilters =
        CVarDef.Create("accessibility.no_vision_filters", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// The offset for in-game date (the date will be server date + this amount of years).
    /// </summary>
    public static readonly CVarDef<int> YearOffset =
        CVarDef.Create("game.current_year_offset", 739, CVar.SERVERONLY);
}

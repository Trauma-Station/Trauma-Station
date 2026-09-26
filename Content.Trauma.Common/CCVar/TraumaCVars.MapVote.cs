// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Trauma.Common.CCVar;

public sealed partial class TraumaCVars
{
    /// <summary>
    /// How many maps are presented in the map vote.
    /// Could be lower if there isn't enough maps for the playercount.
    /// </summary>
    public static readonly CVarDef<int> MapVoteOptions =
        CVarDef.Create("trauma.map_vote_options", 3, CVar.SERVER);

    /// <summary>
    /// If true, the server will automatically start a map vote on round restart
    /// </summary>
    public static readonly CVarDef<bool> AutomaticMapVote =
        CVarDef.Create("trauma.automatic_map_vote", true, CVar.SERVER);
}

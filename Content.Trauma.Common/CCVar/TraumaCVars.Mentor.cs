// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Trauma.Common.CCVar;

public sealed partial class TraumaCVars
{
    public static readonly CVarDef<float> MentorHelpRateLimitPeriod =
        CVarDef.Create("trauma.mentor_help_rate_limit_period", 2f, CVar.SERVERONLY);

    public static readonly CVarDef<int> MentorHelpRateLimitCount =
        CVarDef.Create("trauma.mentor_help_rate_limit_count", 10, CVar.SERVERONLY);

    public static readonly CVarDef<string> MentorHelpSound =
        CVarDef.Create("trauma.mentor_help_sound", "/Audio/_RMC14/Effects/Admin/mhelp.ogg", CVar.ARCHIVE | CVar.CLIENTONLY);
}

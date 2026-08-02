// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Trauma.Common.CCVar;

public sealed partial class TraumaCVars
{
    /// <summary>
    /// Enables effects of knowledge.
    /// When disabled only languages and martial arts will do anything, their levels will be irrelevant.
    /// </summary>
    public static readonly CVarDef<bool> SkillsEnabled =
        CVarDef.Create("trauma.skills_enabled", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Enables gaining XP and skills during rounds.
    /// Character starting skills are not affected by this.
    /// </summary>
    public static readonly CVarDef<bool> SkillGain =
        CVarDef.Create("trauma.skill_gain", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Client setting to hide all skill-related popups.
    /// </summary>
    public static readonly CVarDef<bool> SkillPopups =
        CVarDef.Create("trauma.skill_popups", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}

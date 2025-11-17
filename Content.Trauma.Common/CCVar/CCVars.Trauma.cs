using Robust.Shared.Configuration;

namespace Content.Trauma.Common.CCVar;

[CVarDefs]
public sealed partial class TraumaCVars
{
    #region Slop

    /// <summary>
    ///     Are height/width sliders enabled
    /// </summary>
    public static readonly CVarDef<bool> HeightSliders =
        CVarDef.Create("trauma.height_sliders_enabled", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Is sprinting enabled
    /// </summary>
    public static readonly CVarDef<bool> SprintEnabled =
        CVarDef.Create("trauma.sprint_enabled", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Is antag pity enabled
    /// </summary>
    public static readonly CVarDef<bool> AntagPityEnabled =
        CVarDef.Create("trauma.pity_enabled", false, CVar.SERVER | CVar.REPLICATED);

    #endregion

    #region Mining rewards

    /// <summary>
    /// Maximum currency to possibly give a player from mining in a round.
    /// </summary>
    public static readonly CVarDef<int> MiningRewardLimit =
        CVarDef.Create("trauma.mining_reward_limit", 100, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// How many mining points give 1 currency.
    /// </summary>
    public static readonly CVarDef<int> MiningRewardRatio =
        CVarDef.Create("trauma.mining_reward_ratio", 50, CVar.SERVER | CVar.REPLICATED);

    #endregion
}

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
}

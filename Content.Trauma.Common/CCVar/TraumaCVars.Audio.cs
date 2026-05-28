using Robust.Shared.Configuration;

namespace Content.Trauma.Common.CCVar;

public sealed partial class TraumaCVars
{
    #region Audio

    /// <summary>
    ///     Whether to render sounds with echo when they are in 'large' open, rooved areas.
    /// </summary>
    /// <seealso cref="AreaEchoSystem"/>
    public static readonly CVarDef AreaEchoEnabled =
        CVarDef.Create("trauma.area_echo.enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    ///     If false, area echos calculate with 4 directions (NSEW).
    ///         Otherwise, area echos calculate with all 8 directions.
    /// </summary>
    /// <seealso cref="AreaEchoSystem"/>
    public static readonly CVarDef<bool> AreaEchoHighResolution =
        CVarDef.Create("trauma.area_echo.alldirections", false, CVar.ARCHIVE | CVar.CLIENTONLY);


    /// <summary>
    ///     How many times a ray can bounce off a surface for an echo calculation.
    /// </summary>
    /// <seealso cref="AreaEchoSystem"/>
    public static readonly CVarDef<int> AreaEchoReflectionCount =
        CVarDef.Create("trauma.area_echo.max_reflections", 2, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    ///     Distantial interval, in tiles, in the rays used to calculate the roofs of an open area for echos,
    ///         or the ray's distance to space, at which the tile at that point of the ray is processed.
    ///
    ///     The lower this is, the more 'predictable' and computationally heavy the echoes are.
    /// </summary>
    /// <seealso cref="AreaEchoSystem"/>
    public static readonly CVarDef<float> AreaEchoStepFidelity =
        CVarDef.Create("trauma.area_echo.step_fidelity", 5f, CVar.CLIENTONLY);

    /// <summary>
    ///     Interval between updates for every audio entity.
    /// </summary>
    /// <seealso cref="AreaEchoSystem"/>
    public static readonly CVarDef<TimeSpan> AreaEchoRecalculationInterval =
        CVarDef.Create("trauma.area_echo.recalculation_interval", TimeSpan.FromSeconds(15), CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// Enables advance acoustics, such as audio reverb.
    /// </summary>
    /// <seealso cref="AcousticDataSystem"/>
    public static readonly CVarDef<bool> AcousticEnable =
        CVarDef.Create("trauma.acoustics.enable", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// Whether to cast acoustic rays in four cardinal directions, or eight.
    /// </summary>
    /// <seealso cref="AcousticDataSystem"/>
    public static readonly CVarDef<bool> AcousticHighResolution =
        CVarDef.Create("trauma.acoustics.high_resolution", false, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// How many bounces an acoustic ray may take before ending early.
    /// </summary>
    /// <seealso cref="AcousticDataSystem"/>
    public static readonly CVarDef<int> AcousticReflectionCount =
        CVarDef.Create("trauma.acoustics.reflection_count", 6, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// The minimum value the user can set for vds.acoustics.reflection_count
    /// </summary>
    public static readonly CVarDef<int> AcousticReflectionCountMinimum =
        CVarDef.Create("trauma.acoustics.reflection_count_minimum", 1, CVar.REPLICATED | CVar.SERVER | CVar.CHEAT);

    /// <summary>
    /// The maximum value the user can set for vds.acoustics.reflection_count
    /// </summary>
    public static readonly CVarDef<int> AcousticReflectionCountMaximum =
        CVarDef.Create("trauma.acoustics.reflection_count_maximum", 16, CVar.REPLICATED | CVar.SERVER | CVar.CHEAT);

    #endregion
}

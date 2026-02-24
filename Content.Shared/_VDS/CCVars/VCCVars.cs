using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared._VDS.CCVars;
/// <summary>
/// Vermist Dust Sector specific cvars.
/// </summary>
[CVarDefs]
public sealed class VCCVars
{
    /// <summary>
    /// Enables advance acoustics, such as audio reverb.
    /// </summary>
    /// <seealso cref="AcousticDataSystem"/>
    public static readonly CVarDef<bool> AcousticEnable =
        CVarDef.Create("vds.acoustics.enable", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// Whether to cast acoustic rays in four cardinal directions, or eight.
    /// </summary>
    /// <seealso cref="AcousticDataSystem"/>
    public static readonly CVarDef<bool> AcousticHighResolution =
        CVarDef.Create("vds.acoustics.high_resolution", false, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// How many bounces an acoustic ray may take before ending early.
    /// </summary>
    /// <seealso cref="AcousticDataSystem"/>
    public static readonly CVarDef<int> AcousticReflectionCount =
        CVarDef.Create("vds.acoustics.reflection_count", 6, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// The minimum value the user can set for vds.acoustics.reflection_count
    /// </summary>
    public static readonly CVarDef<int> AcousticReflectionCountMinimum =
        CVarDef.Create("vds.acoustics.reflection_count_minimum", 1, CVar.REPLICATED | CVar.SERVER | CVar.CHEAT);

    /// <summary>
    /// The maximum value the user can set for vds.acoustics.reflection_count
    /// </summary>
    public static readonly CVarDef<int> AcousticReflectionCountMaximum =
        CVarDef.Create("vds.acoustics.reflection_count_maximum", 16, CVar.REPLICATED | CVar.SERVER | CVar.CHEAT);
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Trauma.Common.CCVar;

public sealed partial class TraumaCVars
{
    /// <summary>
    /// Disable vision effect spawning like footsteps, used for integration tests.
    /// </summary>
    public static readonly CVarDef<bool> DisableVisionEffects =
        CVarDef.Create("trauma.disable_vision_effects", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Scale for how strong out-of-vision graininess is, 0 is just pure greyscale.
    /// </summary>
    public static readonly CVarDef<float> VisionGrainScale =
        CVarDef.Create("trauma.vision_grain_scale", 0.75f, CVar.CLIENTONLY | CVar.ARCHIVE);
}

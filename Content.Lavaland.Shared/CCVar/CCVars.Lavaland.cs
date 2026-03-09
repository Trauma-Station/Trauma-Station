// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Lavaland.Shared.CCVar;

[CVarDefs]
public sealed partial class LavalandCVars
{
    /// <summary>
    ///     Should the Lavaland roundstart generation be enabled.
    /// </summary>
    public static readonly CVarDef<bool> LavalandEnabled =
        CVarDef.Create("lavaland.enabled", true, CVar.SERVERONLY);
}

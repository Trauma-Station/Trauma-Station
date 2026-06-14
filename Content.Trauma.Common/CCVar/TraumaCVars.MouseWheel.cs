// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Trauma.Common.CCVar;

public sealed partial class TraumaCVars
{
    /// <summary>
    /// Whether camera zoom can be changed by mouse wheel
    /// </summary>
    public static readonly CVarDef<bool> MouseWheelZoom =
        CVarDef.Create("trauma.mouse_wheel_zoom", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// Whether screen can be rotated by mouse wheel
    /// </summary>
    public static readonly CVarDef<bool> MouseWheelRotate =
        CVarDef.Create("trauma.mouse_wheel_rotate", true, CVar.ARCHIVE | CVar.CLIENTONLY);


    /// <summary>
    /// Whether part targeting can be changed by mousewheel
    /// </summary>
    public static readonly CVarDef<bool> MouseWheelTargeting =
        CVarDef.Create("trauma.mouse_wheel_targeting", true, CVar.ARCHIVE | CVar.CLIENTONLY);
}

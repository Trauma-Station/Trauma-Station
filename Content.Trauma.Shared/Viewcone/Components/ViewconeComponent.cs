// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Viewcone.Components;

/// <summary>
/// Used for players that have a vision cone, they don't get to see anything hard to remember behind them.
/// </summary>
/// <remarks>
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡠⠀⠀⠀⡀⢄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢰⣴⡿⣟⣿⣻⣦⠆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣲⣯⢿⡽⣞⣷⣻⡞⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡈⢿⣽⣯⢿⡽⣞⣷⡻⢥⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⠉⠀⠀⠀⠀⠀⠀⠀⠀⠀⠄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠰⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠐⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢃⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣣⣀⣀⡀⠀⠀⠀⠀⠀⠀⢀⣀⣀⣬⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣌⣿⡽⣯⣟⣿⣻⢟⣿⡻⣟⣯⢿⡽⣯⡡⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠰⣽⣷⣻⢷⣻⢾⣽⣻⢾⣽⣻⣞⣯⣟⡷⣧⡆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢳⣟⡾⣽⢯⡿⣽⢾⣽⣻⣞⣷⣻⢾⣭⣟⡷⡞⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣘⠉⠙⠙⠯⠿⢽⣯⣟⣾⣳⣟⣾⡽⠻⠾⠙⠋⠉⢁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠆⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⢀⣤⣤⡶⣶⣶⡆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢰⣶⣶⢶⣤⣠⡀⠀⠀⠀
/// ⠀⠀⠀⣰⣟⡷⣯⣟⣷⡻⣀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⢷⣯⣟⡾⣽⣳⣆⠀⠀
/// ⠀⠀⢀⡿⣾⣽⣳⣟⡾⢳⣟⣿⣲⢦⣤⣄⣀⣀⠀⠀⠀⠀⠀⠀⢀⣀⣀⣤⣴⢶⡿⣽⡛⣾⣽⢻⣷⣻⢾⡄⠀
/// ⠀⠀⢸⣟⡷⣯⢷⣯⣏⡿⣽⢾⣽⣻⢾⡽⣯⣟⡿⣻⣟⢿⣻⣟⡿⣯⣟⡷⣯⢿⣽⣳⢿⣹⣞⡿⡾⣽⣻⣄⠀
/// ⠀⠀⣿⢾⡽⣯⣟⡾⣼⡽⣯⣟⡾⣽⢯⣟⡷⣯⣟⡷⣯⣟⡷⣯⣟⡷⣯⢿⣽⣻⢾⡽⣯⢧⢯⡿⣽⣳⣟⣎⠀
/// ⠀⢰⣯⢿⣽⣳⢯⡷⣯⣟⡷⣯⢿⣽⣻⢾⣽⣳⢯⣟⡷⣯⣟⡷⣯⢿⣽⣻⢾⡽⣯⢿⣽⣻⢾⣽⣳⣟⡾⣽⠇
/// ⠀⣼⣞⡿⡾⣽⢯⣟⡷⣯⢿⣽⣻⢾⣽⣻⢾⣽⣻⢾⣽⣳⢯⡿⣽⣻⢾⡽⣯⢿⣽⣻⢾⣽⣻⣞⡷⣯⢿⣽⣣
/// ⢠⡾⣽⣻⡽⣯⣟⡾⣽⢯⣟⡾⣽⣻⢾⣽⣻⢾⣽⣻⢾⡽⣯⣟⡷⣯⢿⡽⣯⣟⡾⣽⣻⣞⡷⣯⢿⣽⣻⣞⣷
/// ⠀⠻⣷⣯⣟⡷⣯⢿⣽⣻⢾⣽⣳⢯⣟⡾⣽⣻⢾⡽⣯⣟⡷⣯⢿⡽⣯⣟⡷⣯⣟⣷⣻⢾⡽⣯⣟⣾⣳⢿⡞
/// ⠀⠀⠈⠓⠛⠙⠋⠛⠚⠙⠛⠚⠋⠛⠚⠛⠓⠛⠋⠛⠓⠋⠛⠙⠋⠛⠓⠋⠛⠓⠛⠚⠙⠋⠛⠓⠛⠚⠛⠉⠀
///           THE CONE MAN APPROACHES
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class ViewconeComponent : Component
{
    /// <summary>
    /// Base cone angle, without any modifications (from equipment or otherwise).
    /// </summary>
    /// <remarks>
    /// You probably don't want to refer to this directly if you're using it for actual calculations.
    /// Instead, use <see cref="ViewconeAngleSystem.GetAngle"/>
    /// </remarks>
    [DataField]
    public float BaseConeAngle = 230f; // typical human peripheral vision

    [DataField]
    public float ConeFeather = 24f;

    [DataField]
    public float ConeIgnoreRadius = 1.35f;

    [DataField]
    public float ConeIgnoreFeather = 1.0f;

    /// <summary>
    /// How long it takes for something out of your vision cone to completely fade out, after <see cref="FadeStart"/>.
    /// </summary>
    [DataField]
    public TimeSpan FadeTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Don't start fading until this time has passed.
    /// </summary>
    [DataField]
    public TimeSpan FadeStart = TimeSpan.FromSeconds(0.3);

    // Clientside, used for lerping view angle
    // and keeping it consistent across all overlays
    public Angle ViewAngle;
    public Angle? DesiredViewAngle;
    public Angle LastMouseRotationAngle;
    public Vector2 LastWorldPos;
    public Angle LastWorldRotationAngle;

    /// <summary>
    /// Current cone angle in degrees used by the client when drawing this frame.
    /// </summary>
    public float CurrentConeAngle;
}

// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Viewcone;

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
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ViewconeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ConeAngle = 270f;

    [DataField, AutoNetworkedField]
    public float ConeFeather = 10f;

    [DataField, AutoNetworkedField]
    public float ConeIgnoreRadius = 0.85f;

    [DataField, AutoNetworkedField]
    public float ConeIgnoreFeather = 0.25f;
}

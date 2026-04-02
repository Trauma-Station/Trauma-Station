// <Trauma>
using Robust.Shared.GameStates;
// </Trauma>

namespace Content.Shared.SubFloor;

/// <summary>
/// Added to anyone using <see cref="TrayScannerComponent"/> to handle the vismask changes.
/// </summary>
[RegisterComponent]
[NetworkedComponent] // Trauma - do need to network it...
public sealed partial class TrayScannerUserComponent : Component
{
    /// <summary>
    /// How many t-rays the user is currently using.
    /// </summary>
    [DataField]
    public int Count;
}

using Robust.Shared.GameStates;

namespace Content.Shared.Tabletop.Components;

public sealed partial class TabletopGameComponent
{
    /// <summary>
    /// How many holograms have been spawned onto this board.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int HologramsSpawned;

    /// <summary>
    /// How many holograms are allowed to be spawned total by players.
    /// </summary>
    [DataField]
    public int MaximumHologramsAllowed = 10;
}

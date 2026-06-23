using Content.Shared._Orion.Bitrunning.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Bitrunning.Components;

/// <summary>
/// Stores bitrunning points for a holder, such as an ID card.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(BitrunningPointsSystem))]
[AutoGenerateComponentState]
public sealed partial class BitrunningPointsComponent : Component
{
    /// <summary>
    /// The number of stored bitrunning points.
    /// </summary>
    [DataField, AutoNetworkedField]
    public uint Points;
}

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.ChangeFactionOnEmag;

/// <summary>
/// Changes A entities faction when it gets emaged
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangeFactionOnEmagComponent : Component
{
    [DataField]
    public string Faction;
}

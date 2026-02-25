using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Weapons.Ranged;

/// <summary>
/// Anything with this component will product bullet-holes when shot by a projectile with the BulletholeGeneratorComponent
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BulletholeComponent : Component
{
    [DataField, AutoNetworkedField]
    public int BulletholeCount = 0;

    [DataField, AutoNetworkedField]
    public int BulletholeState;
}

[Serializable, NetSerializable]
public enum BulletholeVisuals
{
    State,
}

[Serializable, NetSerializable]
public enum BulletholeVisualsLayers : byte
{
    Bullethole,
}

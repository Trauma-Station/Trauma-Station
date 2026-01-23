using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Heretic;

/// <summary>
/// Whether this entity has orbiting blades
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ProtectiveBladesComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> Blades = new();
}

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Heretic;

/// <summary>
/// Whether this entity has orbiting blades
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ProtectiveBladesComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> Blades = new();

    [DataField]
    public float ProjectileSpeed = 6.5f;

    [DataField]
    public EntProtoId BlockShootStatus = "BlockProtectiveBladeShootStatusEffect";

    [DataField]
    public TimeSpan BladeShootDelay = TimeSpan.FromMilliseconds(250);
}

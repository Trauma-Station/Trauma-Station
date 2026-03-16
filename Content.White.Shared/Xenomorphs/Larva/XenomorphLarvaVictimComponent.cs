using Content.White.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.White.Shared.Xenomorphs.Larva;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class XenomorphLarvaVictimComponent : Component
{
    [AutoNetworkedField, ViewVariables]
    public ProtoId<InfectionIconPrototype>? InfectedIcon;
}

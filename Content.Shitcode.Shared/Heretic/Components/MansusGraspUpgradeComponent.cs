using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shitcode.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MansusGraspUpgradeComponent : Component
{
    [DataField]
    public ComponentRegistry AddedComponents = new();
}

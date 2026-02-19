using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Medical.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BrainSplattererComponent : Component
{
    [DataField]
    public EntProtoId BrainSplatterDecal = new ("DecalSpawnerGibBrainSplatters");
}

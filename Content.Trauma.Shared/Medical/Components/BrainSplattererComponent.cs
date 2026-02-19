using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Medical.Components;

/// <summary>
/// Creates a brain splatter decal when this part is gibbed
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BrainSplattererComponent : Component
{
    [DataField]
    public EntProtoId BrainSplatterDecal = new ("DecalSpawnerGibBrainSplatters");
}

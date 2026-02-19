using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Trauma - brains splatter when gibbed
/// </summary>
public sealed partial class BodyPartComponent
{
    [DataField]
    public EntProtoId BrainSplatterDecal = new ("DecalSpawnerGibBrainSplatters");
}

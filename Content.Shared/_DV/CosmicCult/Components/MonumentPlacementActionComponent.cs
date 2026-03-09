using Robust.Shared.Prototypes;

namespace Content.Shared._DV.CosmicCult.Components;

[RegisterComponent]
public sealed partial class MonumentPlacementActionComponent : Component
{
    /// <summary>
    /// The mark that was created by this action. If not null, using the action would remove the mark instead.
    /// </summary>
    [DataField]
    public EntityUid? MarkUid;

    [DataField]
    public EntProtoId? MarkProto = "MonumentCosmicCultSpawnMark";
}

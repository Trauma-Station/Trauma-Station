using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._White.ResinNest;

[RegisterComponent]
public sealed partial class ResinNestComponent : Component
{
    [DataField(required: true)]
    public EntProtoId OverlayEntity;

    [DataField]
    public EntityUid? SpawnedOverlay;
}

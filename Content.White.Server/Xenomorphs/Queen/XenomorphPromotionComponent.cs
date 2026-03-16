using Content.Shared.FixedPoint;
using Content.White.Shared.Xenomorphs.Caste;
using Robust.Shared.Prototypes;

namespace Content.White.Server.Xenomorphs.Queen;

[RegisterComponent]
public sealed partial class XenomorphPromotionComponent : Component
{
    [ViewVariables]
    public EntProtoId PromoteTo = "MobXenomorphPraetorian";

    [ViewVariables]
    public FixedPoint2 PlasmaCost = 0;

    [ViewVariables]
    public List<ProtoId<XenomorphCastePrototype>> CasteWhitelist = new();

    [ViewVariables]
    public TimeSpan EvolutionDelay = TimeSpan.FromSeconds(3);
}

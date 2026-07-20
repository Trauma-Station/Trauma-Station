// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Trauma.Shared.Xenomorphs.Caste;

namespace Content.Trauma.Server.Xenomorphs.Queen;

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

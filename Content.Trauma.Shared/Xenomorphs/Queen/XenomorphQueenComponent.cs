// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Xenomorphs.Caste;

namespace Content.Trauma.Shared.Xenomorphs.Queen;

[RegisterComponent]
public sealed partial class XenomorphQueenComponent : Component
{
    [DataField]
    public EntProtoId PromotionActionId = "ActionXenomorphPromotion";

    [DataField]
    public EntProtoId PromoteTo = "MobXenomorphPraetorian";

    [DataField]
    public List<ProtoId<XenomorphCastePrototype>> CasteWhitelist = new() { "Drone", "Hunter", "Sentinel" };

    [DataField]
    public TimeSpan EvolutionDelay = TimeSpan.FromSeconds(3);

    [ViewVariables]
    public EntityUid? PromotionAction;
}

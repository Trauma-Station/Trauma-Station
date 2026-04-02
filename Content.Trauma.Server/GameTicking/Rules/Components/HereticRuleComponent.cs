// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Store;
using Content.Trauma.Server.Heretic.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.Heretic.Components;

[RegisterComponent, Access(typeof(HereticRuleSystem))]
public sealed partial class HereticRuleComponent : Component
{
    [DataField]
    public int RealityShiftPerHeretic = 1;

    [DataField]
    public EntProtoId RealityShift = "EldritchInfluenceT1";

    public readonly List<EntityUid> Minds = new();

    public static readonly List<ProtoId<StoreCategoryPrototype>> StoreCategories = new()
    {
        "HereticPathAsh",
        "HereticPathLock",
        "HereticPathFlesh",
        "HereticPathBlade",
        "HereticPathVoid",
        "HereticPathRust",
        "HereticPathCosmos",
        "HereticPathSideSpecial",
        "HereticPathSideT1",
        "HereticPathSideT2",
        "HereticPathSideT3",
    };
}

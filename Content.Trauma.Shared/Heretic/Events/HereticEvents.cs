using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Trauma.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Heretic.Events;

[ByRefEvent]
public readonly record struct ConsumingFoodEvent(EntityUid Food, FixedPoint2 Volume);

[ByRefEvent]
public record struct ImmuneToPoisonDamageEvent(bool Immune = false);

[ByRefEvent]
public readonly record struct SetGhoulBoundHereticEvent(EntityUid Heretic, EntityUid HereticMind, EntityUid? Ritual);

[ByRefEvent]
public readonly record struct IncrementHereticObjectiveProgressEvent(EntProtoId Proto, int Amount = 1);

[ByRefEvent]
public readonly record struct SpawnHereticInfluenceEvent(EntProtoId Proto, int Amount = 1);

[ByRefEvent]
public readonly record struct UserInvokeTouchSpellEvent;

[DataDefinition]
public sealed partial class EventHereticAscension : EntityEventArgs;

[DataDefinition]
public sealed partial class EventHereticRerollTargets : EntityEventArgs;

[DataDefinition]
public sealed partial class EventHereticUpdateTargets : EntityEventArgs;

[DataDefinition]
public sealed partial class EventHereticResolveStarGazer : EntityEventArgs;

[DataDefinition]
public sealed partial class EventHereticAddKnowledge : EntityEventArgs
{
    [DataField(required: true)]
    public List<ProtoId<HereticKnowledgePrototype>> Knowledge;
}

[DataDefinition]
public sealed partial class HereticModifySideKnowledgeDraftsEvent : EntityEventArgs
{
    [DataField(required: true)]
    public Dictionary<ProtoId<StoreCategoryPrototype>, int> SideKnowledgeDrafts;
}

[DataDefinition]
public sealed partial class HereticGraspUpgradeEvent : EntityEventArgs
{
    [DataField]
    public EntProtoId GraspAction = "ActionHereticMansusGrasp";

    [DataField(required: true)]
    public ComponentRegistry AddedComponents = new();
}

[DataDefinition]
public sealed partial class HereticRemoveActionEvent : EntityEventArgs
{
    [DataField(required: true)]
    public EntProtoId Action;
}

public sealed partial class CrucibleSoulRecallEvent : BaseAlertEvent
{
    [DataField]
    public EntProtoId EffectProto = "StatusEffectCrucibleSoul";
}

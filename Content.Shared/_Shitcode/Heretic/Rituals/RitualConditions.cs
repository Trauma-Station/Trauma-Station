using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Mobs;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitcode.Heretic.Rituals;

public abstract partial class BaseHereticRitualCondition<T> : EntityConditionBase<T> where T : EntityConditionBase<T>
{
    [DataField]
    public LocId? CancelLoc;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return string.Empty;
    }
}

public abstract partial class InputCondition<T> : BaseHereticRitualCondition<T>
    where T : EntityConditionBase<T>
{
    [DataField(required: true)]
    public string InputKey;
}

public abstract partial class OutputCondition<T> : BaseHereticRitualCondition<T>
    where T : EntityConditionBase<T>
{
    [DataField(required: true)]
    public string OutputKey;

    [DataField]
    public bool CancelOnEmptyOutput = true;
}

public abstract partial class InputOutputCondition<T> : OutputCondition<T>
    where T : EntityConditionBase<T>
{
    [DataField(required: true)]
    public string InputKey;
}

public sealed partial class LookupCondition : OutputCondition<LookupCondition>
{
    [DataField]
    public float Range = 1.5f;

    [DataField]
    public LookupFlags Flags = LookupFlags.Uncontained;
}

public sealed partial class FilterHereticsCondition : InputOutputCondition<FilterHereticsCondition>;

public sealed partial class FilterCondition : InputOutputCondition<FilterCondition>
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}

public sealed partial class FilterMobStateCondition : InputOutputCondition<FilterMobStateCondition>
{
    [DataField]
    public MobState MobState = MobState.Alive;

    [DataField]
    public bool InvertCheck = true;
}

public sealed partial class FilterTargetsCondition : InputOutputCondition<FilterTargetsCondition>;

public sealed partial class CombineCondition : InputOutputCondition<CombineCondition>
{
    [DataField(required: true)]
    public string InputKey2;
}

public sealed partial class TakeNumberCondition : InputOutputCondition<TakeNumberCondition>
{
    [DataField(required: true)]
    public int Number;
}

public sealed partial class SacrificeCondition : InputCondition<SacrificeCondition>
{
    [DataField]
    public EntProtoId SacrificeObjective = "HereticSacrificeObjective";

    [DataField]
    public EntProtoId SacrificeHeadObjective = "HereticSacrificeHeadObjective";
}

public sealed partial class SpawnCondition : BaseHereticRitualCondition<SpawnCondition>
{
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Output;
}

public sealed partial class PathBasedSpawnCondition : BaseHereticRitualCondition<PathBasedSpawnCondition>
{
    [DataField(required: true)]
    public EntProtoId FallbackOutput;

    [DataField(required: true)]
    public Dictionary<string, EntProtoId> Output;
}

public sealed partial class ProcessIngredientsCondition : InputCondition<ProcessIngredientsCondition>
{
    [DataField]
    public List<RitualIngredient> Ingredients = new();
}

public sealed partial class RaiseHereticEventCondition : BaseHereticRitualCondition<RaiseHereticEventCondition>
{
    [DataField(required: true), NonSerialized]
    public object? Event;
}

public sealed partial class AddKnowledgeCondition : BaseHereticRitualCondition<AddKnowledgeCondition>
{
    [DataField(required: true)]
    public ProtoId<HereticKnowledgePrototype> Knowledge;
}

public sealed partial class
    FindLostLimitedOutputCondition : OutputCondition<FindLostLimitedOutputCondition>
{
    [DataField]
    public float MinRange = 1.5f;
}

public sealed partial class CanAscendCondition : BaseHereticRitualCondition<CanAscendCondition>;

public sealed partial class ObjectivesCompleteCondition : BaseHereticRitualCondition<ObjectivesCompleteCondition>;

public sealed partial class FilterOnFireCondition : InputOutputCondition<FilterOnFireCondition>;

public sealed partial class FilterHeadlessCondition : InputOutputCondition<FilterHeadlessCondition>;

public sealed partial class FilterReagentPuddleCondition : InputOutputCondition<FilterReagentPuddleCondition>
{
    [DataField]
    public List<ProtoId<ReagentPrototype>> Reagents = new()
    {
        "Blood",
        "AmmoniaBlood",
        "InsectBlood",
        "CopperBlood",
        "ZombieBlood",
        "AlienBlood",
        "BlackBlood",
        "BloodChangeling",
    };

    [DataField]
    public LocId ReagentLoc = "reagent-name-blood";
}

public sealed partial class DeleteEntityHashsetCondition : InputCondition<DeleteEntityHashsetCondition>;

public sealed partial class GhoulifyCondition : InputOutputCondition<GhoulifyCondition>
{
    [DataField]
    public FixedPoint2 TotalHealth = 100f;

    [DataField]
    public bool GiveBlade = true;
}

public sealed partial class AddComponentsCondition : InputCondition<AddComponentsCondition>
{
    [DataField(required: true)]
    public ComponentRegistry Components;
}

public sealed partial class LowTemperatureCondition : BaseHereticRitualCondition<LowTemperatureCondition>
{
    [DataField]
    public float Threshold;
}

public sealed partial class
    FilterKnowledgeTagsCondition : InputOutputCondition<FilterKnowledgeTagsCondition>;

public sealed partial class UpdateKnowledgeCondition : BaseHereticRitualCondition<UpdateKnowledgeCondition>
{
    [DataField(required: true)]
    public float Amount;
}

public sealed partial class RemoveRitualsCondition : BaseHereticRitualCondition<RemoveRitualsCondition>
{
    [DataField(required: true)]
    public List<ProtoId<TagPrototype>> RitualTags = new();
}

public sealed partial class OpenRuneBuiCondition : BaseHereticRitualCondition<OpenRuneBuiCondition>
{
    [DataField(required: true)]
    public Enum Key;
}

public sealed partial class TeleportToRuneCondition : InputCondition<TeleportToRuneCondition>;

public sealed partial class ApplyConditionsCondition : BaseHereticRitualCondition<ApplyConditionsCondition>
{
    /// <summary>
    /// Apply HereticRitualComponent Conditions from and including this index
    /// Skips <see cref="ApplyConditionsCondition"/>
    /// </summary>
    [DataField(required: true)]
    public int FromIndex;

    /// <summary>
    /// Raise HereticRitualComponent Conditions ending with but not including this index
    /// Skips <see cref="ApplyConditionsCondition"/>
    /// </summary>
    [DataField(required: true)]
    public int ToIndex;
}

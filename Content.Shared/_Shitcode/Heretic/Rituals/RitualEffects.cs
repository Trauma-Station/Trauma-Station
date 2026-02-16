using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitcode.Heretic.Rituals;

public abstract partial class BaseRitualEffect<T> : EntityEffectBase<T>, IHereticRitualEntry
    where T : EntityEffectBase<T>
{
    [DataField]
    public string ApplyOn = string.Empty;

    [DataField]
    public EntityCondition[]? IndividualConditions;

    public virtual bool ForceApplyOnRitual => false;

    public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user)
    {
        if (raiser is not HereticRitualRaiser ritualRaiser)
            return;

        if (ApplyOn == string.Empty || ForceApplyOnRitual)
        {
            if (ritualRaiser.TryConditions(target, IndividualConditions))
                base.RaiseEvent(target, raiser, scale, user);
            return;
        }

        foreach (var t in ritualRaiser.GetTargets<EntityUid>(ApplyOn))
        {
            if (!ritualRaiser.TryConditions(t, IndividualConditions))
                continue;

            base.RaiseEvent(t, raiser, scale, user);
        }
    }
}

public abstract partial class OutputRitualEffect<T> : BaseRitualEffect<T> where T : BaseRitualEffect<T>
{
    [DataField(required: true)]
    public string Result;
}

public sealed partial class AddToLimitRitualEffect : OutputRitualEffect<AddToLimitRitualEffect>
{
    public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user)
    {
        if (ApplyOn == string.Empty || ForceApplyOnRitual)
            return;

        if (raiser is not HereticRitualRaiser ritualRaiser)
            return;

        var ritual = ritualRaiser.Ritual;

        if (ritual.Comp.Limit <= 0)
            return;

        var result = new HashSet<EntityUid>();
        foreach (var t in ritualRaiser.GetTargets<EntityUid>(ApplyOn))
        {
            if (ritual.Comp.LimitedOutput.Count >= ritual.Comp.Limit)
                break;

            if (!ritualRaiser.TryConditions(t, IndividualConditions))
                continue;

            ritual.Comp.LimitedOutput.Add(t);
            result.Add(t);
        }

        if (result.Count > 0)
            ritualRaiser.SaveResult(Result, result);
    }
}

public sealed partial class SaveResultRitualEffect : OutputRitualEffect<SaveResultRitualEffect>
{
    public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user)
    {
        if (ApplyOn == string.Empty || ForceApplyOnRitual)
            return;

        if (raiser is not HereticRitualRaiser ritualRaiser)
            return;

        var result = new HashSet<EntityUid>();
        foreach (var t in ritualRaiser.GetTargets<EntityUid>(ApplyOn))
        {
            if (!ritualRaiser.TryConditions(t, IndividualConditions))
                continue;

            result.Add(t);
        }

        ritualRaiser.SaveResult(Result, result);
    }
}

public sealed partial class LookupRitualEffect : OutputRitualEffect<LookupRitualEffect>
{
    [DataField]
    public float Range = 1.5f;

    [DataField]
    public LookupFlags Flags = LookupFlags.Uncontained;
}

public sealed partial class SacrificeEffect : BaseRitualEffect<SacrificeEffect>
{
    [DataField]
    public EntProtoId SacrificeObjective = "HereticSacrificeObjective";

    [DataField]
    public EntProtoId SacrificeHeadObjective = "HereticSacrificeHeadObjective";
}

public sealed partial class EffectsRitualEffect : BaseRitualEffect<EffectsRitualEffect>
{
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;
}

public sealed partial class SpawnRitualEffect : BaseRitualEffect<SpawnRitualEffect>
{
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Output;
}

public sealed partial class PathBasedSpawnEffect : BaseRitualEffect<PathBasedSpawnEffect>
{
    [DataField(required: true)]
    public EntProtoId FallbackOutput;

    [DataField(required: true)]
    public Dictionary<string, EntProtoId> Output;
}

public sealed partial class FindLostLimitedOutputEffect : OutputRitualEffect<FindLostLimitedOutputEffect>
{
    [DataField]
    public float MinRange = 1.5f;
}

public sealed partial class UpdateKnowledgeEffect : BaseRitualEffect<UpdateKnowledgeEffect>
{
    [DataField(required: true)]
    public float Amount;
}

public sealed partial class RemoveRitualsEffect : BaseRitualEffect<RemoveRitualsEffect>
{
    [DataField(required: true)]
    public List<ProtoId<TagPrototype>> RitualTags = new();
}

public sealed partial class OpenRuneBuiEffect : BaseRitualEffect<OpenRuneBuiEffect>
{
    [DataField(required: true)]
    public Enum Key;
}

public sealed partial class TeleportToRuneEffect : BaseRitualEffect<TeleportToRuneEffect>;

public sealed partial class GhoulifyEffect : BaseRitualEffect<GhoulifyEffect>
{
    [DataField]
    public bool GiveBlade = true;

    [DataField]
    public float Health = 150f;

    [DataField]
    public bool CanDeconvert = true;

    [DataField]
    public GhoulDeathBehavior DeathBehavior = GhoulDeathBehavior.NoGib;
}

public sealed partial class SplitIngredientsRitualEffect : BaseRitualEffect<SplitIngredientsRitualEffect>
{
    public override bool ForceApplyOnRitual => true;
}

public sealed partial class IfElseRitualEffect : BaseRitualEffect<IfElseRitualEffect>
{
    public override bool ForceApplyOnRitual => true;

    [DataField(required: true)]
    public EntityEffect[] EffectsA;

    [DataField(required: true)]
    public EntityCondition[] IfConditions;

    [DataField]
    public EntityEffect[]? EffectsB;

    [DataField]
    public string? SaveResultKey;
}

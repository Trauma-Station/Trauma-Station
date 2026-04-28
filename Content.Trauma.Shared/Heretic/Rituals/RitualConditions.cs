// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.EntityConditions;

namespace Content.Trauma.Shared.Heretic.Rituals;

public abstract partial class BaseRitualCondition<T> : EntityConditionBase<T>, IHereticRitualEntry
    where T : EntityConditionBase<T>
{
    [DataField]
    public LocId? CancelLoc;

    [DataField]
    public string ApplyOn = string.Empty;

    public virtual bool ForceApplyOnRitual => false;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return string.Empty;
    }

    public override bool RaiseEvent(EntityUid target, IEntityConditionRaiser raiser)
    {
        if (raiser is not HereticRitualRaiser ritualRaiser)
            return base.RaiseEvent(target, raiser);

        if (ApplyOn == string.Empty || ForceApplyOnRitual)
            return base.RaiseEvent(target, raiser);

        foreach (var t in ritualRaiser.GetTargets<EntityUid>(ApplyOn))
        {
            if (base.RaiseEvent(t, raiser))
                continue;

            if (CancelLoc != null)
                ritualRaiser.SaveResult(SharedHereticRitualSystem.CancelString, Loc.GetString(CancelLoc));

            return false;
        }

        return true;
    }
}

public sealed partial class InputCountCondition : BaseRitualCondition<InputCountCondition>
{
    [DataField]
    public int Min;

    [DataField]
    public int Max = -1;

    [DataField]
    public string Result = string.Empty;

    public override bool RaiseEvent(EntityUid target, IEntityConditionRaiser raiser)
    {
        if (raiser is not HereticRitualRaiser ritualRaiser)
            return false;

        if (ApplyOn == string.Empty || ForceApplyOnRitual)
            return false;

        var input = ritualRaiser.GetTargets<EntityUid>(ApplyOn);

        if (Result == string.Empty)
            return input.Count() >= Min;

        var toSave = Max >= Min ? input.Take(Max).ToHashSet() : input.ToHashSet();

        if (toSave.Count < Min)
        {
            if (CancelLoc != null)
                ritualRaiser.SaveResult(SharedHereticRitualSystem.CancelString, Loc.GetString(CancelLoc));

            return false;
        }

        ritualRaiser.SaveResult(Result, toSave);
        return true;
    }
}

public sealed partial class IsTargetCondition : BaseRitualCondition<IsTargetCondition>;

public sealed partial class ProcessIngredientsCondition : BaseRitualCondition<ProcessIngredientsCondition>
{
    public override bool ForceApplyOnRitual => true;

    [DataField(required: true)]
    public RitualIngredient[] Ingredients = default!;

    [DataField(required: true)]
    public string DeleteEntitiesKey;

    [DataField(required: true)]
    public string SplitEntitiesKey;
}

public sealed partial class CanAscendCondition : BaseRitualCondition<CanAscendCondition>;

public sealed partial class ObjectivesCompleteCondition : BaseRitualCondition<ObjectivesCompleteCondition>;

public sealed partial class FilterKnowledgeTagsCondition : BaseRitualCondition<FilterKnowledgeTagsCondition>
{
    public override bool ForceApplyOnRitual => true;

    [DataField(required: true)]
    public string Result;
}

public sealed partial class TryApplyEffectSequenceCondition : BaseRitualCondition<TryApplyEffectSequenceCondition>
{
    public override bool ForceApplyOnRitual => true;

    [DataField(required: true)]
    public int From;

    [DataField(required: true)]
    public int To;
}

public sealed partial class ConditionsRitualCondition : BaseRitualCondition<ConditionsRitualCondition>
{
    [DataField]
    public bool RequireAll = true;

    [DataField(required: true)]
    public EntityCondition[] Conditions = default!;
}

public sealed partial class IsLimitedOutputCondition : BaseRitualCondition<IsLimitedOutputCondition>;

public sealed partial class HereticMinStageCondition : EntityConditionBase<HereticMinStageCondition>,
    IHereticRitualEntry
{
    [DataField(required: true)]
    public int MinStage;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return string.Empty;
    }
}

public sealed partial class BackstabCondition : EntityConditionBase<BackstabCondition>, IHereticRitualEntry
{
    [DataField]
    public Angle Tolerance = Angle.FromDegrees(45d);

    [DataField]
    public bool ShowPopup = true;

    [DataField]
    public bool PlaySound = true;

    [DataField]
    public bool AlwaysBackstabLaying = true;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return string.Empty;
    }
}

public sealed partial class TryMakeRustWallCondition : EntityConditionBase<TryMakeRustWallCondition>,
    IHereticRitualEntry
{
    [DataField]
    public int? RustStrengthOverride;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return string.Empty;
    }
}

public sealed partial class FleshGhoulLimitCondition : EntityConditionBase<FleshGhoulLimitCondition>,
    IHereticRitualEntry
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return string.Empty;
    }
}

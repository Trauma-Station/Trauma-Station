// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Systems;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Checks if entity is heretic of certain path/stage
/// </summary>
public sealed partial class HereticStageCondition : EntityConditionBase<HereticStageCondition>
{
    [DataField]
    public HereticPath? RequiredPath;

    [DataField]
    public int MinPathStage;

    [DataField]
    public int MaxPathStage = 10;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty;
}

public sealed partial class HereticStageConditionSystem : EntityConditionSystem<MetaDataComponent, HereticStageCondition>
{
    [Dependency] private SharedHereticSystem _heretic = default!;

    protected override void Condition(Entity<MetaDataComponent> ent, ref EntityConditionEvent<HereticStageCondition> args)
    {
        if (!_heretic.TryGetHereticComponent(ent.Owner, out var heretic, out _))
            return;

        args.Result = (args.Condition.RequiredPath is not { } requiredPath || heretic.CurrentPath == requiredPath) &&
                       heretic.PathStage >= args.Condition.MinPathStage && heretic.PathStage <= args.Condition.MaxPathStage;
    }
}

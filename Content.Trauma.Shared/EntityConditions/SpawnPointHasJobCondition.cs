// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.Spawners.Components;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Condition that requires the target player spawn point has a non-null <c>Job</c> set.
/// </summary>
public sealed partial class SpawnPointHasJob : EntityConditionBase<SpawnPointHasJob>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty;
}

public sealed class SpawnPointHasJobSystem : EntityConditionSystem<SpawnPointComponent, SpawnPointHasJob>
{
    protected override void Condition(Entity<SpawnPointComponent> ent, ref EntityConditionEvent<SpawnPointHasJob> args)
    {
        args.Result = ent.Comp.Job != null;
    }
}

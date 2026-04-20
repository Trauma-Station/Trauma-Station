// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Spawners.Components;
using Content.Shared.EntityConditions;
using Content.Trauma.Shared.EntityConditions;

namespace Content.Trauma.Server.EntityConditions;

public sealed class SpawnPointHasJobSystem : EntityConditionSystem<SpawnPointComponent, SpawnPointHasJob>
{
    protected override void Condition(Entity<SpawnPointComponent> ent, ref EntityConditionEvent<SpawnPointHasJob> args)
    {
        args.Result = ent.Comp.Job != null;
    }
}

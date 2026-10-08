// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Objectives.Systems;
using Content.Shared.Objectives.Components;

namespace Content.Trauma.Server.Objectives;

public sealed partial class QueryTargetObjectiveSystem : EntitySystem
{
    [Dependency] private TargetObjectiveSystem _target = default!;

    [SubscribeLocalEvent]
    private void OnAssigned(Entity<QueryTargetObjectiveComponent> ent, ref ObjectiveAssignedEvent args)
    {
        if (FindEntity(ent.Comp.Comp) is not { } target)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent.Owner, target);
    }

    private EntityUid? FindEntity(CompName name)
    {
        var type = Factory.GetRegistration(name).Type;
        var query = EntityManager.GetAllComponents(type);
        foreach (var (uid, _) in query)
        {
            return uid;
        }

        return null;
    }
}

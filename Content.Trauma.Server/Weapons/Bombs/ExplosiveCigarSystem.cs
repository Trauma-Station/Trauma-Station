// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Explosion.EntitySystems;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;
using Content.Shared.Trigger.Systems;
using Content.Trauma.Shared.Weapons.Bombs.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Weapons.Bombs;

public sealed partial class ExplosiveCigarSystem : EntitySystem
{
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private TriggerSystem _trigger = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ExplosiveCigarComponent, SmokableComponent>();
        while (query.MoveNext(out var uid, out var explosive, out var smokable))
        {
            if (explosive.TriggerAtRemaining == null)
                continue;

            if (smokable.State != SmokableState.Lit)
                continue;

            if (_timing.CurTime < explosive.NextCheck)
                continue;

            explosive.NextCheck = _timing.CurTime + explosive.CheckInterval;

            if (!_solutions.TryGetSolution(uid, smokable.Solution, out _, out var solution))
                continue;

            if (solution.Volume <= FixedPoint2.New(explosive.TriggerAtRemaining.Value))
                Explode(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnEmpty(Entity<ExplosiveCigarComponent> ent, ref SmokableSolutionEmptyEvent args)
    {
        if (ent.Comp.TriggerAtRemaining == null)
            Explode(ent);
    }

    private void Explode(EntityUid uid)
    {
        EntityUid? user = null;
        if (_containers.TryGetContainingContainer(uid, out var container))
            user = container.Owner;
        else
        {
            var parent = Transform(uid).ParentUid;
            if (parent.IsValid())
                user = parent;
        }

        _trigger.Trigger(uid, user, "timer");

        _explosion.TriggerExplosive(uid);
        QueueDel(uid);
    }
}

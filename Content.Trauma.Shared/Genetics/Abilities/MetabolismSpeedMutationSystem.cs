// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Metabolism;
using Content.Trauma.Shared.Genetics.Abilities;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.Genetics.Abilities;

public sealed partial class MetabolismSpeedMutationSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private EntityQuery<MetabolizerComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetabolismSpeedMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<MetabolismSpeedMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<MetabolismSpeedMutationComponent> ent, ref MutationAddedEvent args)
    {
        Modify(args.Target, ent.Comp.Bonus);
    }

    private void OnRemoved(Entity<MetabolismSpeedMutationComponent> ent, ref MutationRemovedEvent args)
    {
        Modify(args.Target, -ent.Comp.Bonus);
    }

    private void Modify(EntityUid uid, float add)
    {
        // some shitcode mobs like dragon have metabolizer on the mob itself not organs, check edge case
        if (_query.TryComp(uid, out var mobComp))
        {
            mobComp.UpdateIntervalMultiplier += add;
            Dirty(uid, mobComp);
        }

        foreach (var organ in _body.GetOrgans<MetabolizerComponent>(uid))
        {
            organ.Comp.UpdateIntervalMultiplier += add;
            Dirty(organ);
        }
    }
}

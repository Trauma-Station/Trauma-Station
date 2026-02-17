using Content.Shared.Body;
using Content.Shared.Metabolism;
using Content.Trauma.Shared.Genetics.Abilities;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.Genetics.Abilities;

public sealed class MetabolismSpeedMutationSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;

    private EntityQuery<MetabolizerComponent> _query;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<MetabolizerComponent>();

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

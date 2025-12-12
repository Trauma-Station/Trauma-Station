using Content.Server.Body.Components;
using Content.Shared.Body.Systems;
using Content.Trauma.Shared.Genetics.Abilities;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.Genetics.Abilities;

// TODO: move this to shared if metabolizer is refactored to shared
public sealed class MetabolismSpeedMutationSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;

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
        Modify(ent, ent.Comp.Bonus);
    }

    private void OnRemoved(Entity<MetabolismSpeedMutationComponent> ent, ref MutationRemovedEvent args)
    {
        Modify(ent, -ent.Comp.Bonus);
    }

    private void Modify(EntityUid uid, float add)
    {
        // some shitcode mobs like dragon have metabolizer on the mob itself not organs, check edge case
        if (_query.TryComp(uid, out var mobComp))
        {
            mobComp.UpdateIntervalMultiplier += add;
            // TODO: dirty it if refactored
            //Dirty(uid, mobComp);
        }

        foreach (var (organ, _) in _body.GetBodyOrgans(uid))
        {
            if (!_query.TryComp(organ, out var comp))
                continue;

            comp.UpdateIntervalMultiplier += add;
            // TODO: dirty it if refactored
            // Dirty(organ, comp);
        }
    }
}

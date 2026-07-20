// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Temperature.Components;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.Genetics.Abilities;

public sealed partial class TemperatureDamageMutationSystem : EntitySystem
{
    [Dependency] private EntityQuery<TemperatureDamageComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemperatureDamageMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<TemperatureDamageMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<TemperatureDamageMutationComponent> ent, ref MutationAddedEvent args)
    {
        if (!_query.TryComp(args.Target, out var comp))
            return;

        comp.ColdDamageThreshold += ent.Comp.ColdOffset;
        comp.HeatDamageThreshold += ent.Comp.HeatOffset;
    }

    private void OnRemoved(Entity<TemperatureDamageMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (!_query.TryComp(args.Target, out var comp))
            return;

        comp.ColdDamageThreshold -= ent.Comp.ColdOffset;
        comp.HeatDamageThreshold -= ent.Comp.HeatOffset;
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Body.Components;
using Content.Trauma.Shared.Genetics.Abilities;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Server.Genetics.Abilities;

public sealed class ThermalRegulatorMutationSystem : EntitySystem
{
    [Dependency] private readonly EntityQuery<ThermalRegulatorComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalRegulatorMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<ThermalRegulatorMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<ThermalRegulatorMutationComponent> ent, ref MutationAddedEvent args)
    {
        if (!_query.TryComp(args.Target, out var comp))
            return;

        comp.ShiveringHeatRegulation *= ent.Comp.Shivering;
        comp.SweatHeatRegulation *= ent.Comp.Sweating;
        comp.MetabolismHeat *= ent.Comp.Metabolism;
        comp.ImplicitHeatRegulation *= ent.Comp.Regulation;
    }

    private void OnRemoved(Entity<ThermalRegulatorMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (!_query.TryComp(args.Target, out var comp))
            return;

        comp.ShiveringHeatRegulation /= ent.Comp.Shivering;
        comp.SweatHeatRegulation /= ent.Comp.Sweating;
        comp.MetabolismHeat /= ent.Comp.Metabolism;
        comp.ImplicitHeatRegulation /= ent.Comp.Regulation;
    }
}

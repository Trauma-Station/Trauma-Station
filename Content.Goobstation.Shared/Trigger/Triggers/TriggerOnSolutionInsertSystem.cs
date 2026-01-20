// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Fishbait <Fishbait@git.ml>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 fishbait <gnesse@gmail.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Containers;

namespace Content.Goobstation.Shared.Trigger.Triggers;

public sealed class TriggerOnSolutionInsertSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnSolutionInsertComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
    }

    private void OnEntInserted(Entity<TriggerOnSolutionInsertComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        var quantity = RecursiveCheckForSolution(args.Entity, ent.Comp.Reagent);
        if ((ent.Comp.MinAmount is not {} min || quantity >= min) &&
            (ent.Comp.MaxAmount is not {} max || quantity <= max))
        {
            _trigger.Trigger(ent, user: null, ent.Comp.KeyOut);
        }
    }

    //Gonna get recursive up in here
    private FixedPoint2 RecursiveCheckForSolution(EntityUid uid, string reagent)
    {
        var solutionFound = FixedPoint2.Zero;
        // TODO: this is fucking evil use an event instead
        if (TryComp<ContainerManagerComponent>(uid, out var containerManager))
        {
            foreach (var (id, container) in containerManager.Containers)
            {
                foreach (var ent in container.ContainedEntities)
                {
                    solutionFound += RecursiveCheckForSolution(ent, reagent);
                }
            }
        }
        return solutionFound + _solution.GetTotalPrototypeQuantity(uid, reagent);
    }
}

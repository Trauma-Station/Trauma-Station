// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;

namespace Content.Trauma.Shared.Chemistry;

public sealed class VialHypospraySystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VialHyposprayComponent, InjectorGetSolutionEvent>(OnGetSolution);
    }

    private void OnGetSolution(Entity<VialHyposprayComponent> ent, ref InjectorGetSolutionEvent args)
    {
        Log.Debug($"GetSolution for {ToPrettyString(ent)}");
        if (args.Handled)
            return;

        args.Handled = true;
        if (_slots.GetItemOrNull(ent.Owner, ent.Comp.Slot) is not {} vial)
            return;

        Log.Debug($"Vial {ToPrettyString(vial)}");
        _solution.TryGetDrawableSolution(vial, out var solution, out _);
        Log.Debug($"Solution {ToPrettyString(solution)}");
        args.Solution = solution;
    }
}

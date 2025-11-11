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

        SubscribeLocalEvent<VialHyposprayComponent, HyposprayGetSolutionEvent>(OnGetSolution);
    }

    private void OnGetSolution(Entity<VialHyposprayComponent> ent, ref HyposprayGetSolutionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (_slots.GetItemOrNull(ent.Owner, ent.Comp.Slot) is not {} vial)
            return;

        _solution.TryGetDrawableSolution(vial, out var solution, out _);
        args.Solution = solution;
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Goobstation.Shared.Chemistry;

public sealed partial class SolutionCartridgeSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CartridgeInjectorComponent, InjectorGetSolutionEvent>(OnGetSolution);
        SubscribeLocalEvent<CartridgeInjectorComponent, AfterInjectedEvent>(OnInjected);
    }

    private void OnGetSolution(Entity<CartridgeInjectorComponent> ent, ref InjectorGetSolutionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (!_container.TryGetContainer(ent, "item", out var container) ||
            container is not ContainerSlot slot ||
            slot.ContainedEntity is not {} item ||
            !TryComp<SolutionCartridgeComponent>(item, out var cartridge) ||
            !_solution.TryGetSolution(item, cartridge.SolutionName, out var solution))
            return;

        args.Solution = solution;
    }

    private void OnInjected(Entity<CartridgeInjectorComponent> ent, ref AfterInjectedEvent args)
    {
        if (!_container.TryGetContainer(ent, "item", out var container))
            return;

        _container.CleanContainer(container);
    }
}

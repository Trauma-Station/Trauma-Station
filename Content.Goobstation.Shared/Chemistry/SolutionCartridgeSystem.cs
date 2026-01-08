// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Ted Lukin <66275205+pheenty@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Network;

namespace Content.Goobstation.Shared.Chemistry;

public sealed class SolutionCartridgeSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CartridgeInjectorComponent, EntInsertedIntoContainerMessage>(OnCartridgeInserted);
        SubscribeLocalEvent<CartridgeInjectorComponent, EntRemovedFromContainerMessage>(OnCartridgeRemoved);
        SubscribeLocalEvent<CartridgeInjectorComponent, AfterInjectedEvent>(OnInjected);
    }

    private void OnCartridgeInserted(Entity<CartridgeInjectorComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<SolutionCartridgeComponent>(args.Entity, out var cartridge)
        || !TryComp(ent, out SolutionContainerManagerComponent? manager)
        || !_solution.TryGetSolution((ent, manager), cartridge.TargetSolution, out var solutionEntity))
            return;

        if (_timing.ApplyingState)
            return;

        _solution.TryAddSolution(solutionEntity.Value, cartridge.Solution);
    }

    private void OnCartridgeRemoved(Entity<CartridgeInjectorComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!TryComp<SolutionCartridgeComponent>(args.Entity, out var cartridge)
        || !TryComp(ent, out SolutionContainerManagerComponent? manager)
        || !_solution.TryGetSolution((ent, manager), cartridge.TargetSolution, out var solutionEntity))
            return;

        if (_timing.ApplyingState)
            return;

        _solution.RemoveAllSolution(solutionEntity.Value);
    }

    private void OnInjected(Entity<CartridgeInjectorComponent> ent, ref AfterInjectedEvent args)
    {
        if (!_container.TryGetContainer(ent, "item", out var container))
            return;

        if (_net.IsClient)
            return;

        _container.CleanContainer(container);
    }
}

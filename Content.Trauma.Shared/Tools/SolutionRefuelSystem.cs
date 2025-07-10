using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.Tools;

public sealed partial class SolutionRefuelSystemSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private bool TryGetSolutionFuelAndCapacity(EntityUid uid, out FixedPoint2 fuel, out FixedPoint2 capacity)
    {
        fuel = default;
        capacity = default;
        if (!TryComp<SolutionRefuelComponent>(uid, out var refuelable) ||
            !TryComp<SolutionManagerComponent>(uid, out var solutionContainer))
            return false;

        if (!_solution.TryGetSolution(
                (uid, solutionContainer),
                refuelable.FuelSolutionName,
                out _,
                out var fuelSolution))
            return false;

        fuel = fuelSolution.GetTotalPrototypeQuantity(refuelable.FuelReagent);
        capacity = fuelSolution.MaxVolume;
        return true;
    }

    [SubscribeLocalEvent]
    private void SolutionRefuelExamine(Entity<SolutionRefuelComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange ||
            !TryGetSolutionFuelAndCapacity(ent.Owner, out var fuel, out var capacity))
            return;

        args.PushMarkup(Loc.GetString("solution-refuel-component-on-examine-detailed-message",
                ("colorName", fuel < capacity / FixedPoint2.New(4f) ? "darkorange" : "orange"),
                ("fuelLeft", fuel),
                ("fuelCapacity", capacity)));
    }

    [SubscribeLocalEvent]
    private void OnSolutionRefuelAfterInteract(Entity<SolutionRefuelComponent> ent, ref AfterInteractEvent args)
    {
        // Wowza that's a lot of conditions
        if (args.Target is not { } target ||
            args.Handled ||
            !args.CanReach ||
            !TryComp<ReagentTankComponent>(target, out var tank) ||
            tank.TankType != ReagentTankType.Fuel ||
            !_solution.TryGetDrainableSolution(target, out _, out var targetSolution) ||
            !_solution.TryGetSolution(ent.Owner, ent.Comp.FuelSolutionName, out var solutionComp, out var welderSolution))
            return;

        args.Handled = true;

        // Oh those imp players
        var trans = FixedPoint2.Min(welderSolution.AvailableVolume, targetSolution.GetTotalPrototypeQuantity(ent.Comp.FuelReagent));
        if (trans > 0)
        {
            var drained = targetSolution.SplitSolutionWithOnly(trans, ent.Comp.FuelReagent);
            _solution.TryAddSolution(solutionComp.Value, drained);
            _audio.PlayPredicted(ent.Comp.WelderRefill, ent, user: args.User);
            _popup.PopupClient(Loc.GetString("welder-component-after-interact-refueled-message"), ent, args.User);
        }
        else if (welderSolution.AvailableVolume <= 0)
        {
            _popup.PopupClient(Loc.GetString("solution-refuel-component-already-full", ("name", GetName(ent))), ent, args.User);
        }
        else
        {
            _popup.PopupClient(Loc.GetString("welder-component-no-fuel-in-tank", ("owner", args.Target)), ent, args.User);
        }

    }

    private string GetName(Entity<SolutionRefuelComponent> ent)
    {
        if (ent.Comp.Name == null)
            return Identity.Name(ent, EntityManager);
        return Loc.GetString(ent.Comp.Name);
    }
}

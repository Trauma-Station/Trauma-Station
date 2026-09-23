// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Goobstation.Shared.Chemistry;

/// <summary>
/// Contains all the logic for reagent dispensers.
/// <seealso cref="EnergyReagentDispenserComponent"/>
/// </summary>
public sealed partial class EnergyReagentDispenserSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private EntityQuery<BatteryComponent> _batteryQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<EnergyReagentDispenserComponent>(EnergyReagentDispenserUiKey.Key, subs =>
        {
            subs.Event<EnergyReagentDispenserSetDispenseAmountMessage>(OnSetDispenseAmount);
            subs.Event<EnergyReagentDispenserDispenseReagentMessage>(OnDispenseReagent);
            subs.Event<EnergyReagentDispenserClearContainerSolutionMessage>(OnClearContainerSolution);
        });
    }

    [SubscribeLocalEvent]
    private void OnInit(Entity<EnergyReagentDispenserComponent> ent, ref ComponentInit args)
    {
        ent.Comp.BeakerSlot = _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.OutputSlotName);
    }

    private void OnSetDispenseAmount(Entity<EnergyReagentDispenserComponent> ent, ref EnergyReagentDispenserSetDispenseAmountMessage args)
    {
        var amount = args.Amount;
        if (ent.Comp.DispenseAmount == amount || amount > ent.Comp.MaxDispenseAmount || amount < ent.Comp.MinDispenseAmount)
            return;

        ent.Comp.DispenseAmount = amount;
        Dirty(ent);
        ClickSound(ent, args.Actor);
    }

    private void OnDispenseReagent(Entity<EnergyReagentDispenserComponent> ent, ref EnergyReagentDispenserDispenseReagentMessage args)
    {
        if (ent.Comp.Beaker is not { } item ||
            !_solution.TryGetFitsInDispenser(item, out var solution, out var sol) ||
            !ProtoMan.HasIndex(args.ReagentId) ||
            !_batteryQuery.TryComp(ent, out var battery))
            return;

        var user = args.Actor;
        var amount = FixedPoint2.Min(FixedPoint2.New(ent.Comp.DispenseAmount), sol.AvailableVolume);
        if (amount <= FixedPoint2.Zero)
        {
            _popup.PopupCursor("No room left for liquids!", user);
            return;
        }

        var powerRequired = GetEnergyCostForReagent(args.ReagentId, amount, ent.Comp);
        var charge = _battery.GetCharge((ent, battery));
        if (charge < powerRequired)
        {
            _audio.PlayPredicted(ent.Comp.PowerSound, ent, user);
            return;
        }

        var adding = new Solution(args.ReagentId, amount);
        if (!_solution.TryAddSolution(solution.Value, adding))
            return;

        _battery.UseCharge((ent, battery), powerRequired);
        ClickSound(ent, user);
    }

    private void OnClearContainerSolution(Entity<EnergyReagentDispenserComponent> ent, ref EnergyReagentDispenserClearContainerSolutionMessage args)
    {
        if (ent.Comp.Beaker is not { } item ||
            !_solution.TryGetFitsInDispenser(item, out var solution, out var soln))
            return;

        var refundedEnergy = soln.Sum(reagent => GetEnergyCostForReagent(reagent.Reagent.Prototype, reagent.Quantity, ent));
        if (refundedEnergy > 0)
            _battery.ChangeCharge(ent.Owner, refundedEnergy);

        _solution.RemoveAllSolution(solution.Value);
        ClickSound(ent, args.Actor);
    }

    private void ClickSound(Entity<EnergyReagentDispenserComponent> ent, EntityUid user)
    {
        _audio.PlayPredicted(ent.Comp.ClickSound, ent, user);
    }

    private static float GetEnergyCostForReagent(ProtoId<ReagentPrototype> reagentId, FixedPoint2 amount, EnergyReagentDispenserComponent comp)
        => comp.Reagents.TryGetValue(reagentId, out var cost)
            ? (amount * cost).Float()
            : 0f;
}

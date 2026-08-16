// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Server.Chemistry.Components;
using Content.Goobstation.Shared.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Power;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Content.Shared.Labels.Components;
using Content.Server.Power.Components;
using Robust.Shared.Player;
using Content.Server.Power.EntitySystems;
using Content.Shared.Power.Components;

namespace Content.Goobstation.Server.Chemistry.EntitySystems;

// TODO: predict all this shit
/// <summary>
/// Contains all the server-side logic for reagent dispensers.
/// <seealso cref="EnergyReagentDispenserComponent"/>
/// </summary>
public sealed partial class EnergyReagentDispenserSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private BatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergyReagentDispenserComponent, ComponentStartup>(SubscribeUpdateUiState);
        SubscribeLocalEvent<EnergyReagentDispenserComponent, SolutionChangedEvent>(SubscribeUpdateUiState);
        SubscribeLocalEvent<EnergyReagentDispenserComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
        SubscribeLocalEvent<EnergyReagentDispenserComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
        SubscribeLocalEvent<EnergyReagentDispenserComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

        SubscribeLocalEvent<EnergyReagentDispenserComponent, EnergyReagentDispenserSetDispenseAmountMessage>(OnSetDispenseAmountMessage);
        SubscribeLocalEvent<EnergyReagentDispenserComponent, EnergyReagentDispenserDispenseReagentMessage>(OnDispenseReagentMessage);
        SubscribeLocalEvent<EnergyReagentDispenserComponent, EnergyReagentDispenserClearContainerSolutionMessage>(OnClearContainerSolutionMessage);
        SubscribeLocalEvent<EnergyReagentDispenserComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<EnergyReagentDispenserComponent, MapInitEvent>(OnMapInit, before: [typeof(ItemSlotsSystem)]);
    }

    private void SubscribeUpdateUiState<T>(Entity<EnergyReagentDispenserComponent> ent, ref T ev) => UpdateUiState(ent);

    private void UpdateUiState(Entity<EnergyReagentDispenserComponent> ent)
    {
        var outputContainer = _slots.GetItemOrNull(ent.Owner, SharedEnergyReagentDispenser.OutputSlotName);
        var outputContainerInfo = BuildOutputContainerInfo(outputContainer);
        var inventory = GetInventory(ent.Comp);
        var batteryCharge = 0f;
        var batteryMaxCharge = 0f;
        var currentReceivingEnergy = 0f;
        var usingBattery = false;
        var idleUse = 0f;
        var hasPower = false;

        if (TryComp<BatteryComponent>(ent, out var batteryComp))
        {
            batteryCharge = _battery.GetCharge((ent, batteryComp));
            batteryMaxCharge = batteryComp.MaxCharge;
        }

        if (TryComp<ApcPowerReceiverBatteryComponent>(ent, out var apcPower))
        {
            currentReceivingEnergy = apcPower.BatteryRechargeRate;
            usingBattery = apcPower.Enabled;
            idleUse = apcPower.IdleLoad;
        }

        if (TryComp<ApcPowerReceiverComponent>(ent, out var apc))
            hasPower = apc.Powered;

        var state = new EnergyReagentDispenserBoundUserInterfaceState(
            outputContainerInfo,
            GetNetEntity(outputContainer),
            inventory,
            ent.Comp.DispenseAmount,
            batteryCharge,
            batteryMaxCharge,
            currentReceivingEnergy,
            idleUse,
            usingBattery,
            hasPower
        );
        _ui.SetUiState(ent.Owner, EnergyReagentDispenserUiKey.Key, state);
    }

    private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
    {
        if (container is not { Valid: true })
            return null;

        if (_solution.TryGetFitsInDispenser(container.Value, out _, out var solution))
        {
            return new ContainerInfo(Name(container.Value), solution.Volume, solution.MaxVolume)
            {
                Reagents = solution.Contents,
            };
        }

        return null;
    }

    private List<EnergyReagentInventoryItem> GetInventory(EnergyReagentDispenserComponent comp)
    {
        var inventory = new List<EnergyReagentInventoryItem>();

        foreach (var (reagentId, cost) in comp.Reagents)
        {
            if (!ProtoMan.TryIndex<ReagentPrototype>(reagentId, out var reagentProto))
                continue;

            inventory.Add(new EnergyReagentInventoryItem(
                reagentId,
                reagentProto.LocalizedName,
                cost,
                reagentProto.SubstanceColor
            ));
        }

        inventory.Sort((a, b) => string.Compare(a.ReagentLabel, b.ReagentLabel, StringComparison.Ordinal));
        return inventory;
    }

    private void OnSetDispenseAmountMessage(Entity<EnergyReagentDispenserComponent> ent, ref EnergyReagentDispenserSetDispenseAmountMessage args)
    {
        var amount = args.Amount;
        if (ent.Comp.DispenseAmount == amount || amount > ent.Comp.MaxDispenseAmount || amount < ent.Comp.MinDispenseAmount)
            return;

        ent.Comp.DispenseAmount = amount;
        UpdateUiState(ent);
        ClickSound(ent);
    }

    private void OnPowerChanged(Entity<EnergyReagentDispenserComponent> ent, ref PowerChangedEvent args) =>
        UpdateUiState(ent);

    private void OnDispenseReagentMessage(Entity<EnergyReagentDispenserComponent> ent, ref EnergyReagentDispenserDispenseReagentMessage message)
    {
        var outputContainer = _slots.GetItemOrNull(ent.Owner, SharedEnergyReagentDispenser.OutputSlotName);
        if (outputContainer is not { Valid: true }
            || !_solution.TryGetFitsInDispenser(outputContainer.Value, out var solution, out _))
            return;

        if (!TryComp<BatteryComponent>(ent, out var batteryComp))
            return;

        var amount = (int) ent.Comp.DispenseAmount;
        var powerRequired = GetPowerCostForReagent(message.ReagentId, amount, ent.Comp);
        var currentCharge = _battery.GetCharge((ent, batteryComp));

        if (currentCharge < powerRequired)
        {
            _audio.PlayPvs(ent.Comp.PowerSound, ent, AudioParams.Default.WithVolume(-2f));
            return;
        }


        var sol = new Solution(message.ReagentId, amount);
        if (!_solution.TryAddSolution(solution.Value, sol))
            return;

        _battery.SetCharge(ent.Owner, currentCharge - powerRequired);
        ClickSound(ent);
        UpdateUiState(ent);
    }

    private void OnClearContainerSolutionMessage(Entity<EnergyReagentDispenserComponent> ent, ref EnergyReagentDispenserClearContainerSolutionMessage message)
    {
        var outputContainerNullable = _slots.GetItemOrNull(ent.Owner, SharedEnergyReagentDispenser.OutputSlotName);
        if (outputContainerNullable is not { Valid: true } outputContainer
            || !_solution.TryGetFitsInDispenser(outputContainer, out var solution, out var soln))
            return;

        var refundedPower = soln.Sum(reagent => GetPowerCostForReagent(reagent.Reagent.Prototype, (int) reagent.Quantity, ent));
        var currentCharge = _battery.GetCharge(ent.Owner);
        if (refundedPower > 0)
            _battery.SetCharge(ent.Owner, currentCharge + refundedPower);

        _solution.RemoveAllSolution(solution.Value);
        UpdateUiState(ent);
        ClickSound(ent);
    }

    private void ClickSound(Entity<EnergyReagentDispenserComponent> ent) =>
        _audio.PlayPvs(ent.Comp.ClickSound, ent, AudioParams.Default.WithVolume(-2f));

    private static float GetPowerCostForReagent(string reagentId, int amount, EnergyReagentDispenserComponent comp)
        => comp.Reagents.TryGetValue(reagentId, out var cost)
            ? cost * amount
            : 0f;

    private void OnMapInit(Entity<EnergyReagentDispenserComponent> ent, ref MapInitEvent args)
    {
        _slots.AddItemSlot(ent.Owner, SharedEnergyReagentDispenser.OutputSlotName, ent.Comp.EnergyBeakerSlot);
    }
}

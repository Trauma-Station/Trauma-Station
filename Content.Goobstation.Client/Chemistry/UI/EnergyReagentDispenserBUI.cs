// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Goobstation.Shared.Chemistry;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;

namespace Content.Goobstation.Client.Chemistry.UI;

/// <summary>
/// Initializes a <see cref="EnergyReagentDispenserWindow"/>.
/// </summary>
[UsedImplicitly]
public sealed class EnergyReagentDispenserBUI(EntityUid owner, Enum key) : BoundUserInterface(owner, key)
{
    [ViewVariables]
    private EnergyReagentDispenserWindow? _window;

    /// <summary>
    /// Called each time a dispenser UI instance is opened. Generates the dispenser window and fills it with
    /// relevant info. Sets the actions for static buttons.
    /// <para>Buttons which can change like reagent dispense buttons have their actions set in <see cref="UpdateReagentsList"/>.</para>
    /// </summary>
    protected override void Open()
    {
        base.Open();

        // Setup window info
        _window = this.CreateWindow<EnergyReagentDispenserWindow>();
        var comp = EntMan.GetComponent<EnergyReagentDispenserComponent>(Owner);
        _window.SetInfoFromEntity(EntMan, Owner);
        _window.SetOwner(Owner, comp);

        // Handle button actions.
        _window.OnEjectBeaker += () => SendPredictedMessage(new ItemSlotButtonPressedEvent(comp.OutputSlotName));
        _window.OnClearBeaker += () => SendPredictedMessage(new EnergyReagentDispenserClearContainerSolutionMessage());

        _window.OnSetAmount += i => SendPredictedMessage(new EnergyReagentDispenserSetDispenseAmountMessage(i));

        _window.OnDispenseReagent += id => SendPredictedMessage(new EnergyReagentDispenserDispenseReagentMessage(id));
    }
}

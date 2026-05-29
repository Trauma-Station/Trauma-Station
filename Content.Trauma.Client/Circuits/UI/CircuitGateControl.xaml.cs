// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Lathe.UI;
using Content.Trauma.Shared.Circuits;
using Robust.Client.ResourceManagement;
using Robust.Shared.Input;

namespace Content.Trauma.Client.Circuits.UI;

[GenerateTypedNameReferences]
public sealed partial class CircuitGateControl : PanelContainer
{
    public readonly CircuitGate Gate;
    public readonly int Index;

    public event Action<CircuitPinControl>? OnPinPressed;
    public event Action? OnDrag;
    public event Action? OnDrop;
    public event Action? OnRemove;

    public CircuitGateControl(IResourceCache cache, CircuitGate gate, int index)
    {
        RobustXamlLoader.Load(this);

        Gate = gate;
        Index = index;

        NameLabel.Text = gate.Name;
        AddTooltip(NameContainer, gate);

        for (var n = 0; n < gate.Inputs.Count; n++)
        {
            var inPin = new CircuitPinControl(cache, CircuitIndex.Gate(index), n);
            inPin.OnPressed += pin => OnPinPressed?.Invoke(pin);
            InputsContainer.AddChild(inPin);
        }

        // only 1 output per gate for now
        var pin = new CircuitPinControl(cache, CircuitIndex.Gate(index), -1);
        pin.OnPressed += pin => OnPinPressed?.Invoke(pin);
        OutputsContainer.AddChild(pin);

        // drag/drop name to move the gate
        NameContainer.OnKeyBindDown += args =>
        {
            if (args.Function != EngineKeyFunctions.UIClick)
                return;

            args.Handle();
            OnDrag?.Invoke();
        };
        NameContainer.OnKeyBindUp += args =>
        {
            if (args.Function != EngineKeyFunctions.UIClick)
                return;

            args.Handle();
            OnDrop?.Invoke();
        };

        RemoveButton.OnPressed += _ => OnRemove?.Invoke();
    }

    public CircuitPinControl? GetPin(int i, bool output)
    {
        var pins = output ? OutputsContainer : InputsContainer;
        return i < pins.ChildCount
            ? (CircuitPinControl) pins.Children[i]
            : null;
    }

    public static void AddTooltip(Control control, CircuitGate gate)
    {
        // if it works it works
        var tooltip = new RecipeTooltip(gate.Desc);
        control.TooltipSupplier = _ => tooltip;
    }
}

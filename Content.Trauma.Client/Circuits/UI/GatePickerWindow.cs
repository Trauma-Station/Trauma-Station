// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Trauma.Shared.Circuits;

namespace Content.Trauma.Client.Circuits.UI;

/// <summary>
/// A window that can result in creating a <see cref="CircuitGate"/> from user input.
/// </summary>
public abstract class GatePickerWindow : FancyWindow
{
    public event Action<CircuitGate>? OnPicked;

    protected void Pick(CircuitGate gate)
    {
        OnPicked?.Invoke(gate);
        Close();
    }
}

/// <summary>
/// A window that can create a <see cref="CircuitConstantGate"/> from user input.
/// </summary>
public abstract class ConstPickerWindow : GatePickerWindow
{
    public abstract object Value { get; }

    public void Create()
    {
        Pick(new CircuitConstantGate(Value));
    }
}

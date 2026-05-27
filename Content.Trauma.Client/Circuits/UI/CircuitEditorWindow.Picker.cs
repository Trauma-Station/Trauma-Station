// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Circuits;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;

namespace Content.Trauma.Client.Circuits.UI;

public sealed partial class CircuitEditorWindow
{
    [Dependency] private IReflectionManager _reflection = default!;
    [Dependency] private ISandboxHelper _sandbox = default!;

    private GatePickerWindow? _constWindow;

    private void SetupPicker()
    {
        OnClose += () => _constWindow?.Close();

        var types = _reflection.GetAllChildren<CircuitGate>();
        var gates = new Dictionary<string, List<CircuitGate>>();
        foreach (var type in types)
        {
            var gate = (CircuitGate) _sandbox.CreateInstance(type);
            var cat = gate.Category;
            if (cat == string.Empty)
                continue; // ignore hidden ones

            var list = gates.GetOrNew(cat);
            gate.AddVariants(list);
        }

        NewGatesContainer.AddChild(new Label()
        {
            Text = "Constants"
        });
        AddConstButton<BoolPickerWindow>("Bool");
        AddConstButton<IntPickerWindow>("Integer");
        AddConstButton<StringPickerWindow>("String");

        foreach (var (cat, list) in gates)
        {
            NewGatesContainer.AddChild(new Label()
            {
                Text = cat
            });

            foreach (var gate in list)
            {
                var evil = gate; // dogshit language...
                var button = new Button();
                button.Text = gate.Name;
                button.OnPressed += _ => OnAddGate?.Invoke(evil);
                CircuitGateControl.AddTooltip(button, gate);
                NewGatesContainer.AddChild(button);
            }
        }
    }

    private void AddConstButton<T>(string name) where T : GatePickerWindow, new()
    {
        var button = new Button();
        button.Text = name;
        button.OnPressed += _ =>
        {
            _constWindow?.Close();
            // using new T() makes roslyn emit System.Activator stuff which sandbox forbids, do it with SandboxManager instead
            _constWindow = (T) _sandbox.CreateInstance(typeof(T));
            _constWindow.OpenCentered();
            _constWindow.OnPicked += gate => OnAddGate?.Invoke(gate);
            _constWindow.OnClose += () =>
            {
                _constWindow = null;
            };
        };
        NewGatesContainer.AddChild(button);
    }
}

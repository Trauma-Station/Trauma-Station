// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Circuits;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;

namespace Content.Trauma.Client.Circuits.UI;

public sealed partial class CircuitEditorWindow
{
    [Dependency] private IReflectionManager _reflection = default!;
    [Dependency] private ISandboxHelper _sandbox = default!;

    private void SetupPicker()
    {
        var types = _reflection.GetAllChildren<CircuitGate>();
        var gates = new Dictionary<string, List<CircuitGate>>();
        foreach (var type in types)
        {
            var gate = (CircuitGate) _sandbox.CreateInstance(type);
            var cat = gate.Category;
            var list = gates.GetOrNew(cat);
            gate.AddVariants(list);
        }

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
                NewGatesContainer.AddChild(button);
            }
        }
    }
}

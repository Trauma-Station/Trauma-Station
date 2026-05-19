// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Circuits;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Circuits.UI;

public sealed partial class CircuitEditorBUI(EntityUid owner, Enum key) : BoundUserInterface(owner, key)
{
    private CircuitEditorWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CircuitEditorWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is CircuitEditorState cast)
            _window?.UpdateState(cast);
    }
}

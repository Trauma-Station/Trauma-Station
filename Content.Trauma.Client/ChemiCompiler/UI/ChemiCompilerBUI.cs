// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.ChemiCompiler;

namespace Content.Trauma.Client.ChemiCompiler.UI;

public sealed class ChemiCompilerBUI : BoundUserInterface
{
    private ChemiCompilerWindow? _window;

    public ChemiCompilerBUI(EntityUid owner, Enum key) : base(owner, key)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ChemiCompilerWindow>();

        // the material list reads the machine's storage itself, it isn't part of the ui state
        _window.SetOwner(Owner);

        // programs only exist on the server, so none of these can be predicted
        _window.OnSave += (slot, code) => SendMessage(new ChemiCompilerSaveMessage(slot, code));
        _window.OnRun += slot => SendMessage(new ChemiCompilerRunMessage(slot));
        _window.OnHalt += () => SendMessage(new ChemiCompilerHaltMessage());

        // inserting and ejecting beakers goes through item slots, which are predicted
        _window.OnReservoir += reservoir => SendPredictedMessage(new ChemiCompilerReservoirMessage(reservoir));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ChemiCompilerState cast)
            _window?.UpdateState(cast);
    }
}

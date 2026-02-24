// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.Autodoc;
using Robust.Client.UserInterface;

namespace Content.Medical.Client.Autodoc;

public sealed class AutodocBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private AutodocWindow? _window;

    public AutodocBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AutodocWindow>();
        _window.SetOwner(Owner);

        _window.OnCreateProgram += title => SendMessage(new AutodocCreateProgramMessage(title));
        _window.OnToggleProgramSafety += program => SendMessage(new AutodocToggleProgramSafetyMessage(program));
        _window.OnRemoveProgram += program => SendMessage(new AutodocRemoveProgramMessage(program));

        _window.OnAddStep += (program, step, index) => SendMessage(new AutodocAddStepMessage(program, step, index));
        _window.OnRemoveStep += (program, stepIndex) => SendMessage(new AutodocRemoveStepMessage(program, stepIndex));

        _window.OnImportProgram += (program) => SendMessage(new AutodocImportProgramMessage(program));

        _window.OnStart += program => SendMessage(new AutodocStartMessage(program));
        _window.OnStop += () => SendMessage(new AutodocStopMessage());
    }
}

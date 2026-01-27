// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Research.Components;
using Content.Trauma.Shared.Genetics.Console;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Genetics.UI;

public sealed class GeneticsConsoleBUI : BoundUserInterface
{
    private GeneticsConsoleWindow? _window;

    public GeneticsConsoleBUI(EntityUid owner, Enum key) : base(owner, key)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GeneticsConsoleWindow>();
        _window.SetEntity(Owner);
        _window.OpenCentered();
        _window.OnSelectServer += () => SendPredictedMessage(new ConsoleServerSelectionMessage());
        _window.OnScan += () => SendPredictedMessage(new GeneticsConsoleScanMessage());
        _window.OnScramble += () => SendPredictedMessage(new GeneticsConsoleScrambleMessage());
        _window.OnSetBase += (s, i, c) => SendPredictedMessage(new GeneticsConsoleSetBaseMessage(s, i, c));
        _window.OnWriteMutation += i => SendPredictedMessage(new GeneticsConsoleWriteMutationMessage(i));
        _window.OnSequence += i => SendPredictedMessage(new GeneticsConsoleSequenceMessage(i));
        _window.OnPrint += p => SendPredictedMessage(new GeneticsConsolePrintMessage(p));
        _window.OnCombine += i => SendPredictedMessage(new GeneticsConsoleCombineMessage(i));
        _window.OnSaveEnzymes += () => SendPredictedMessage(new GeneticsConsoleSaveEnzymesMessage());
        _window.OnApplyEnzymes += () => SendPredictedMessage(new GeneticsConsoleApplyEnzymesMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not GeneticsConsoleState cast)
            return;

        _window?.UpdateState(cast);
    }
}

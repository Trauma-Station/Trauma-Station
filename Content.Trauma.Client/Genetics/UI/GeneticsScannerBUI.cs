// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Research.Components;
using Content.Trauma.Shared.Genetics.Console;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Genetics.UI;

public sealed class GeneticsScannerBUI : BoundUserInterface
{
    private GeneticsScannerWindow? _window;

    public GeneticsScannerBUI(EntityUid owner, Enum key) : base(owner, key)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GeneticsScannerWindow>();
        _window.SetEntity(Owner);
        _window.OpenCentered();
        _window.OnScan += () => SendPredictedMessage(new GeneticsConsoleScanMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not GeneticsConsoleState cast)
            return;

        _window?.UpdateState(cast);
    }
}

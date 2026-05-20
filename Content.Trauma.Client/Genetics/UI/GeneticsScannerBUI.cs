// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Research.Components;
using Content.Trauma.Shared.Genetics.Console;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Genetics.UI;

public sealed partial class GeneticsScannerBUI(EntityUid owner, Enum key) : BoundUserInterface(owner, key)
{
    private GeneticsScannerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GeneticsScannerWindow>();
        _window.SetEntity(Owner);
        _window.OnScan += () => SendPredictedMessage(new GeneticsConsoleScanMessage());
        _window.OnPrint += i => SendPredictedMessage(new GeneticsPrintScanMessage(i));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (_window is not {} window || state is not GeneticsConsoleState cast)
            return;

        window.UpdateState(cast);
        if (!window.IsOpen)
            window.OpenCentered();
    }
}

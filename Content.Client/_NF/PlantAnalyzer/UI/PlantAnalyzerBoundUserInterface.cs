// SPDX-FileCopyrightText: 2025 Liamofthesky <157073227+Liamofthesky@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 ReconPangolin <67752926+ReconPangolin@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Shared._NF.PlantAnalyzer;
using Content.Shared.Botany.Components;
using JetBrains.Annotations;

namespace Content.Client._NF.PlantAnalyzer.UI;

[UsedImplicitly]
public sealed class PlantAnalyzerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PlantAnalyzerWindow? _window;

    public PlantAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new PlantAnalyzerWindow(this)
        {
            Title = Loc.GetString("plant-analyzer-interface-title"),
        };
        _window.OnClose += Close;
        _window.OpenCenteredLeft();
    }

    protected override void UpdateState(BoundUserInterfaceState state)  //Funkystation - Switched to state instead of message to fix UI bug
    {
        if (_window == null)
            return;

        if (state is PlantAnalyzerScannedSeedPlantInformation cast)  //Funkystation - Switched to state instead of message to fix UI bug
            _window.Populate(cast);
        if (state is PlantAnalyzerCurrentMode mast)
            _window.Populate(mast);
        if (state is PlantAnalyzerCurrentCount last)
            _window.Populate(last);
        if (state is PlantAnalyzerSeedDatabank seed)
            _window.Populate(seed);
        return;
    }

    public void AdvPressed(PlantAnalyzerModes scanMode)
    {
        if (_window != null)
        {
            _window.internalmode = scanMode;
            SendMessage(new PlantAnalyzerSetMode(scanMode));
        }
    }

    public void GeneIterate(bool up)
    {
        if (_window!=null)
            SendMessage(new PlantAnalyzerMutateIterate(up, (_window.internalmode == PlantAnalyzerModes.Implant)));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_window != null)
            _window.OnClose -= Close;

        _window?.Dispose();
    }
    public void DeleteDatabaseEntry()
    {
        SendMessage(new PlantAnalyzerDeleteDatabankEntry());
    }
}

// SPDX-FileCopyrightText: 2025 Liamofthesky <157073227+Liamofthesky@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 ReconPangolin <67752926+ReconPangolin@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Trauma.Shared.Botany.PlantAnalyzer;
using Content.Trauma.Shared.Botany.Components;

namespace Content.Trauma.Client.Botany.PlantAnalyzer.UI;


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
        SendMessage(new PlantAnalyzerRequestDefault());
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
        _window.PopulateUpdateButtons();
        return;
    }

    public void AdvPressed(PlantAnalyzerModes scanMode)
    {
        if (_window != null)
        {
            _window._internalmode = scanMode;
            _window.PopulateUpdateButtons();
            SendMessage(new PlantAnalyzerSetMode(scanMode));
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_window != null)
            _window.OnClose -= Close;

        _window?.Orphan();
    }
    public void DeleteDatabaseEntry(int index)
    {
        if (index < 0)
            return;
        if (_window != null)
        {
            _window._internalDatabaseNumber += -1;
            SendMessage(new PlantAnalyzerDeleteDatabankEntry(index));
        }
    }

    public void SetGeneIndex(int index)
    {
        if (index < 0)
            return;
        if (_window != null)
        {
            _window._internalGeneNumber = index;
            SendMessage(new PlantAnalyzerSetGeneIndex(_window._internalGeneNumber, false));
        }
}

    public void SetDatabaseIndex(int index)
    {
        if (index < 0)
            return;
        if (_window != null)
        {
            _window._internalGeneNumber = index - 1;
            SendMessage(new PlantAnalyzerSetGeneIndex(index, true));
        }
    }
}

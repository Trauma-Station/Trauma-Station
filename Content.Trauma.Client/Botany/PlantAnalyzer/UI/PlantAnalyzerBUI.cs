// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Botany.PlantAnalyzer;

namespace Content.Trauma.Client.Botany.PlantAnalyzer.UI;

public sealed partial class PlantAnalyzerBUI(EntityUid owner, Enum key) : BoundUserInterface(owner, key)
{
    [ViewVariables]
    private PlantAnalyzerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PlantAnalyzerWindow>();
        _window.OnSetMode += mode => SendPredictedMessage(new PlantAnalyzerSetMode(mode));
        _window.OnSelectGene += i => SendPredictedMessage(new PlantAnalyzerSetGeneIndex(i, true));
        _window.OnSelectEntry += i => SendPredictedMessage(new PlantAnalyzerSetGeneIndex(i, false));
        _window.OnDeleteEntry += () => SendPredictedMessage(new PlantAnalyzerDeleteDatabankEntry());
        _window.SetOwner(Owner);
        _window.OpenCenteredLeft();
    }
}

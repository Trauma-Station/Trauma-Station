// SPDX-FileCopyrightText: 2025 Liamofthesky <157073227+Liamofthesky@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using System.Linq;
using Content.Shared.Atmos;
using Content.Trauma.Server.Botany;
using Content.Trauma.Shared.Botany.Components;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;

namespace Content.Trauma.Server.Botany.Components;

/// <summary>
///    After scanning, retrieves the target Uid to use with its related UI.
/// </summary>
[RegisterComponent]
public sealed partial class PlantAnalyzerComponent : Component
{
    [DataDefinition]
    public partial struct PlantAnalyzerSetting
    {
        [DataField]
        public PlantAnalyzerModes AnalyzerModes;

        [DataField]
        public float ScanDelay;

        [DataField]
        public float AdvScanDelay;
    }

    [DataField]
    public PlantAnalyzerSetting Settings = new();

    [DataField]
    public DoAfterId? DoAfter;

    [DataField]
    public SoundSpecifier? ScanningEndSound;

    [DataField]
    public List<GeneData> GeneBank = new();

    [DataField]
    public List<GasData> ConsumeGasesBank = new();

    [DataField]
    public List<GasData> ExudeGasesBank = new();

    [DataField]
    public List<ChemData> ChemicalBank = new();


    [DataField]
    public int GeneIndex = 0;

    [DataField]
    public int DatabankIndex = 0;
}

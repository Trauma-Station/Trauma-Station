// SPDX-FileCopyrightText: 2025 Liamofthesky <157073227+Liamofthesky@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Shared.DoAfter;
using Content.Shared.Random;
using Content.Trauma.Shared.Botany.Components;
using Robust.Shared.Audio;

namespace Content.Trauma.Server.Botany.Components;

/// <summary>
///    After scanning, retrieves the target Uid to use with its related UI.
/// </summary>
[RegisterComponent]
public sealed partial class PlantAnalyzerComponent : Component
{
    [DataField]
    public PlantAnalyzerSetting Settings = new();

    [DataField]
    public DoAfterId? DoAfter;

    [DataField]
    public SoundSpecifier? ScanningEndSound;

    [DataField]
    public SoundSpecifier? DeleteMutationEndSound;

    [DataField]
    public SoundSpecifier? ExtractEndSound;

    [DataField]
    public SoundSpecifier? InjectEndSound;

    [DataField]
    public List<GeneData> GeneBank = new();

    [DataField]
    public List<GasData> ConsumeGasesBank = new();

    [DataField]
    public List<GasData> ExudeGasesBank = new();

    [DataField]
    public List<ChemData> ChemicalBank = new();

    [DataField]
    public List<RandomPlantMutation> MutationBank= new();

    [DataField]
    public List<string> StoredMutationStrings = new();

    [DataField]
    public int GeneIndex = 0;

    [DataField]
    public int DatabankIndex = 0;
}

[DataRecord]
public partial struct PlantAnalyzerSetting
{
    public PlantAnalyzerModes AnalyzerModes;

    public float ScanDelay;

    public float ModeDelay;
}

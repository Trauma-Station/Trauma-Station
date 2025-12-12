// SPDX-FileCopyrightText: 2025 Liamofthesky <157073227+Liamofthesky@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Content.Shared.Atmos;
using Content.Shared.Botany.Components;

namespace Content.Server.Botany.Components;

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

    [DataField, ViewVariables]
    public PlantAnalyzerSetting Settings = new();

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public DoAfterId? DoAfter;

    [DataField]
    public SoundSpecifier? ScanningEndSound;

    [DataField]
    public List<GeneData> MutationBank = new();

    [DataField]
    public int MutationIndex = 0;

    [DataField]
    public int DatabankIndex = 0;

    // This is some shit which is really fucking wack.
    public float GetGeneFromInteger(int index, SeedData seed)
    {
        if (index < 0)
        {
            return 0.0f;
        }

        Dictionary<int, float> seedData = new()
        {
            { 0, seed.NutrientConsumption},
            { 1, seed.WaterConsumption },
            { 2, seed.IdealHeat },
            { 3, seed.HeatTolerance },
            { 4, seed.IdealLight },
            { 5, seed.LightTolerance },
            { 6, seed.ToxinsTolerance },
            { 7, seed.LowPressureTolerance },
            { 8, seed.HighPressureTolerance },
            { 9, seed.PestTolerance },
            { 10, seed.WeedTolerance },
            { 11, seed.Endurance },
            { 12, (float) seed.Yield },
            { 13, seed.Lifespan },
            { 14, seed.Maturation },
            { 15, seed.Production },
            { 16, seed.GrowthStages },
            { 17, (float) seed.HarvestRepeat },
            { 18, seed.Potency },
            { 19, (float)Convert.ToInt16(seed.Seedless) },
            { 20, (float)Convert.ToInt16(seed.Viable) },
            { 21, (float)Convert.ToInt16(seed.Ligneous) },
            { 22, (float)Convert.ToInt16(seed.CanScream) },
            { 23, (float)Convert.ToInt16(seed.TurnIntoKudzu) }
        };
        return seedData[index];
    }

    public void SetGeneFromInteger(int index, SeedData seed)
    {
        GeneData mutation = MutationBank[index];
        switch (mutation.MutationID)
        {
            case 0:
                {
                    seed.NutrientConsumption = mutation.MutationValue;
                    break;
                }
            case 1:
                {
                    seed.WaterConsumption = mutation.MutationValue;
                    break;
                }
            case 2:
                {
                    seed.IdealHeat = mutation.MutationValue;
                    break;
                }
            case 3:
                {
                    seed.HeatTolerance = mutation.MutationValue;
                    break;
                }
            case 4:
                {
                    seed.IdealLight = mutation.MutationValue;
                    break;
                }
            case 5:
                {
                    seed.LightTolerance = mutation.MutationValue;
                    break;
                }
            case 6:
                {
                    seed.ToxinsTolerance = mutation.MutationValue;
                    break;
                }
            case 7:
                {
                    seed.LowPressureTolerance = mutation.MutationValue;
                    break;
                }
            case 8:
                {
                    seed.HighPressureTolerance = mutation.MutationValue;
                    break;
                }
            case 9:
                {
                    seed.PestTolerance = mutation.MutationValue;
                    break;
                }
            case 10:
                {
                    seed.WeedTolerance = mutation.MutationValue;
                    break;
                }
            case 11:
                {
                    seed.Endurance = mutation.MutationValue;
                    break;
                }
            case 12:
                {
                    seed.Yield = (int) mutation.MutationValue;
                    break;
                }
            case 13:
                {
                    seed.Lifespan = mutation.MutationValue;
                    break;
                }
            case 14:
                {
                    seed.Maturation = mutation.MutationValue;
                    break;
                }
            case 15:
                {
                    seed.Production = mutation.MutationValue;
                    break;
                }
            case 16:
                {
                    seed.GrowthStages = (int) mutation.MutationValue;
                    break;
                }
            case 17:
                {
                    seed.HarvestRepeat = (HarvestType) mutation.MutationValue;
                    break;
                }
            case 18:
                {
                    seed.Potency = mutation.MutationValue;
                    break;
                }
            case 19:
                {
                    seed.Seedless = Convert.ToBoolean(mutation.MutationValue);
                    break;
                }
            case 20:
                {
                    seed.Viable = Convert.ToBoolean(mutation.MutationValue);
                    break;
                }
            case 21:
                {
                    seed.Ligneous = Convert.ToBoolean(mutation.MutationValue);
                    break;
                }
            case 22:
                {
                    seed.CanScream = Convert.ToBoolean(mutation.MutationValue);
                    break;
                }
            case 23:
                {
                    seed.TurnIntoKudzu = Convert.ToBoolean(mutation.MutationValue);
                    break;
                }
        }
    }
}

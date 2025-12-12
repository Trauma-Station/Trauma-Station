// SPDX-FileCopyrightText: 2025 Liamofthesky <157073227+Liamofthesky@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using System.Linq;
using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Content.Shared.DoAfter;
using JetBrains.FormatRipper.Elf;
using Robust.Shared.Audio;
using Serilog;

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

    // This is some shit which is really fucking wack.
    public void GetGeneFromInteger(int index, SeedData seed)
    {
        if (index < 0)
        {
            return;
        }

        int intCount = SeedDataTypes.IdToType.Count;
        if (index >= intCount)
        {
            if (index >= intCount + 1)
            {
                if (index >= intCount + 2)
                {
                    foreach (KeyValuePair<string, SeedChemQuantity> chemical in seed.Chemicals)
                    {
                        ChemicalBank.Add(new ChemData(chemical.Key, new SeedChemQuantityAlternate(chemical.Value.Min, chemical.Value.Max, chemical.Value.PotencyDivisor, chemical.Value.Inherent)));
                    }
                }
                else
                {
                    foreach (KeyValuePair<Gas, float> gas in seed.ExudeGasses)
                    {
                        ExudeGasesBank.Add(new GasData(gas.Key, gas.Value));
                    }
                }
            }
            else
            {
                foreach (KeyValuePair<Gas, float> gas in seed.ConsumeGasses)
                {
                    ConsumeGasesBank.Add(new GasData(gas.Key, gas.Value));
                }
            }
        }
        else
        {
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
            GeneBank.Add(new GeneData(index, seedData[index]));
        }
    }

    public void SetGeneFromInteger(int index, SeedData seed)
    {
        int intCount = 0;
        if (index >= intCount + GeneBank.Count)
        {
            intCount += GeneBank.Count;
            if (index >= intCount + ConsumeGasesBank.Count)
            {
                intCount += ConsumeGasesBank.Count;
                if (index >= intCount + ExudeGasesBank.Count)
                {
                    intCount += ExudeGasesBank.Count;
                    ChemData chem = ChemicalBank[index - intCount];
                    SeedChemQuantity chemical = new SeedChemQuantity();
                    chemical.Min = chem.ChemValue.Min;
                    chemical.Max = chem.ChemValue.Max;
                    chemical.PotencyDivisor = chem.ChemValue.PotencyDivisor;
                    chemical.Inherent = chem.ChemValue.Inherent;
                    seed.Chemicals.Add(chem.ChemID, chemical);
                }
                else
                {
                    GasData gas = ExudeGasesBank[index - intCount];
                    seed.ExudeGasses.Add(gas.GasID, gas.GasValue);
                }
            }
            else
            {
                GasData gas = ConsumeGasesBank[index - intCount];
                seed.ConsumeGasses.Add(gas.GasID, gas.GasValue);
            }
        }
        else
        {
            GeneData gene = GeneBank[index];
            switch (gene.GeneID)
            {
                case 0:
                    {
                        seed.NutrientConsumption = gene.GeneValue;
                        break;
                    }
                case 1:
                    {
                        seed.WaterConsumption = gene.GeneValue;
                        break;
                    }
                case 2:
                    {
                        seed.IdealHeat = gene.GeneValue;
                        break;
                    }
                case 3:
                    {
                        seed.HeatTolerance = gene.GeneValue;
                        break;
                    }
                case 4:
                    {
                        seed.IdealLight = gene.GeneValue;
                        break;
                    }
                case 5:
                    {
                        seed.LightTolerance = gene.GeneValue;
                        break;
                    }
                case 6:
                    {
                        seed.ToxinsTolerance = gene.GeneValue;
                        break;
                    }
                case 7:
                    {
                        seed.LowPressureTolerance = gene.GeneValue;
                        break;
                    }
                case 8:
                    {
                        seed.HighPressureTolerance = gene.GeneValue;
                        break;
                    }
                case 9:
                    {
                        seed.PestTolerance = gene.GeneValue;
                        break;
                    }
                case 10:
                    {
                        seed.WeedTolerance = gene.GeneValue;
                        break;
                    }
                case 11:
                    {
                        seed.Endurance = gene.GeneValue;
                        break;
                    }
                case 12:
                    {
                        seed.Yield = (int) gene.GeneValue;
                        break;
                    }
                case 13:
                    {
                        seed.Lifespan = gene.GeneValue;
                        break;
                    }
                case 14:
                    {
                        seed.Maturation = gene.GeneValue;
                        break;
                    }
                case 15:
                    {
                        seed.Production = gene.GeneValue;
                        break;
                    }
                case 16:
                    {
                        seed.GrowthStages = (int) gene.GeneValue;
                        break;
                    }
                case 17:
                    {
                        seed.HarvestRepeat = (HarvestType) gene.GeneValue;
                        break;
                    }
                case 18:
                    {
                        seed.Potency = gene.GeneValue;
                        break;
                    }
                case 19:
                    {
                        seed.Seedless = Convert.ToBoolean(gene.GeneValue);
                        break;
                    }
                case 20:
                    {
                        seed.Viable = Convert.ToBoolean(gene.GeneValue);
                        break;
                    }
                case 21:
                    {
                        seed.Ligneous = Convert.ToBoolean(gene.GeneValue);
                        break;
                    }
                case 22:
                    {
                        seed.CanScream = Convert.ToBoolean(gene.GeneValue);
                        break;
                    }
                case 23:
                    {
                        seed.TurnIntoKudzu = Convert.ToBoolean(gene.GeneValue);
                        break;
                    }
            }
        }
    }
}

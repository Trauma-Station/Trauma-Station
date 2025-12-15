// SPDX-FileCopyrightText: 2025 Liamofthesky <157073227+Liamofthesky@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 ReconPangolin <67752926+ReconPangolin@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using System.Linq;
using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Shared.Atmos;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.PowerCell;
using Content.Trauma.Server.Botany.Components;
using Content.Trauma.Shared.Botany.Components;
using Content.Trauma.Shared.Botany.PlantAnalyzer;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;

namespace Content.Trauma.Server.Botany.Systems;

public sealed class PlantAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerSetMode>(OnModeSelected);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerGeneIterate>(OnGeneIterate);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerDeleteDatabankEntry>(OnDeleteDatabaseEntry);
    }

    private void OnAfterInteract(Entity<PlantAnalyzerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (!args.CanReach || !_cell.HasActivatableCharge(ent.Owner, user: args.User))
            return;

        if (ent.Comp.DoAfter != null)
            return;

        if (HasComp<SeedComponent>(target) || TryComp<PlantHolderComponent>(target, out var plantHolder) && plantHolder.Seed != null)
        {

            if (ent.Comp.Settings.AnalyzerModes == PlantAnalyzerModes.AdvancedScan)
            {
                var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.Settings.AdvScanDelay, new PlantAnalyzerDoAfterEvent(), ent, target: target, used: ent)
                {
                    NeedHand = true,
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    MovementThreshold = 0.01f
                };
                _doAfterSystem.TryStartDoAfter(doAfterArgs, out ent.Comp.DoAfter);
            }
            else
            {
                var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.Settings.ScanDelay, new PlantAnalyzerDoAfterEvent(), ent, target: target, used: ent)
                {
                    NeedHand = true,
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    MovementThreshold = 0.01f
                };
                _doAfterSystem.TryStartDoAfter(doAfterArgs, out ent.Comp.DoAfter);
            }
        }
    }

    private void OnDoAfter(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerDoAfterEvent args)
    {
        ent.Comp.DoAfter = null;
        // Double charge use for advanced scan.
        if (ent.Comp.Settings.AnalyzerModes == PlantAnalyzerModes.AdvancedScan)
        {
            if (!_cell.TryUseActivatableCharge(ent.Owner, user: args.User))
                return;
        }
        if (args.Handled || args.Cancelled || args.Args.Target == null || !_cell.TryUseActivatableCharge(ent.Owner, user: args.User))
            return;

        _audio.PlayPvs(ent.Comp.ScanningEndSound, ent);

        if ((ent.Comp.Settings.AnalyzerModes == PlantAnalyzerModes.AdvancedScan) || (ent.Comp.Settings.AnalyzerModes == PlantAnalyzerModes.BasicScan))
        {
            ReadScannedPlant(ent, args.Args.Target.Value); //Funkystation - Renamed to match plants instead of copying HealthAnalyzer func names
        }
        if (ent.Comp.Settings.AnalyzerModes == PlantAnalyzerModes.Extract)
        {
            ExtractGene(ent, args.Args.Target.Value);
        }
        if (ent.Comp.Settings.AnalyzerModes == PlantAnalyzerModes.Implant)
        {
            InjectGene(ent, args.Args.Target.Value);
        }
        OpenUserInterface(args.User, ent);

        args.Handled = true;
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!TryComp<ActorComponent>(user, out var actor) || !_uiSystem.HasUi(analyzer, PlantAnalyzerUiKey.Key))
            return;

        _uiSystem.OpenUi(analyzer, PlantAnalyzerUiKey.Key, actor.PlayerSession);
    }

    public void ExtractGene(Entity<PlantAnalyzerComponent> ent, EntityUid target)
    {
        if (ent.Comp.GeneIndex < 0)
            return;
        if (TryComp<SeedComponent>(target, out var seedComp))
        {
            if (seedComp.Seed != null)
            {
                // Copy genes to databank.
                GetGeneFromInteger(ent, seedComp.Seed);
                // Delete seed
                Del(target);
            }
            else if (seedComp.SeedId != null && _prototypeManager.TryIndex(seedComp.SeedId, out SeedPrototype? protoSeed))
            {
                // Copy genes to databank.
                GetGeneFromInteger(ent, protoSeed);
                // Delete seed
                Del(target);
            }
        }
        else if (TryComp<PlantHolderComponent>(target, out var plantComp))
        {
            if (plantComp.Seed != null)
            {
                // Copy genes to databank.
                GetGeneFromInteger(ent, plantComp.Seed);
                // Del(target);
            }
        }
        _uiSystem.SetUiState(ent.Owner, PlantAnalyzerUiKey.Key, new PlantAnalyzerSeedDatabank(ent.Comp.GeneBank, ent.Comp.ConsumeGasesBank, ent.Comp.ExudeGasesBank, ent.Comp.ChemicalBank));
    }

    public void InjectGene(Entity<PlantAnalyzerComponent> ent, EntityUid target)
    {
        if (ent.Comp.DatabankIndex < 0 || ent.Comp.DatabankIndex >= ent.Comp.GeneBank.Count + ent.Comp.ConsumeGasesBank.Count + ent.Comp.ExudeGasesBank.Count + ent.Comp.ChemicalBank.Count)
            return;
        if (TryComp<SeedComponent>(target, out var seedComp))
        {
            if (seedComp.Seed != null)
            {
                SetGeneFromInteger(ent, ref seedComp.Seed);
            }
            else
            {
                _prototypeManager.TryIndex(seedComp.SeedId, out SeedPrototype? protoSeed);
                seedComp.Seed = protoSeed.Clone();
                SetGeneFromInteger(ent, ref seedComp.Seed);
            }
        }
        else if (TryComp<PlantHolderComponent>(target, out var plantComp))
        {
            if (plantComp.Seed != null)
            {
                SetGeneFromInteger(ent, ref plantComp.Seed);
            }
        }
        _uiSystem.SetUiState(ent.Owner, PlantAnalyzerUiKey.Key, new PlantAnalyzerSeedDatabank(ent.Comp.GeneBank, ent.Comp.ConsumeGasesBank, ent.Comp.ExudeGasesBank, ent.Comp.ChemicalBank));
    }
    public void ReadScannedPlant(Entity<PlantAnalyzerComponent> ent, EntityUid target)  //Funkystation - Renamed to match plants instead of copying HealthAnalyzer func names
    {

        if (TryComp<SeedComponent>(target, out var seedComp))
        {
            if (seedComp.Seed != null)
            {
                var state = ObtainingGeneDataSeed(seedComp.Seed, target, false, ent.Comp.Settings.AnalyzerModes);
                _uiSystem.SetUiState(ent.Owner, PlantAnalyzerUiKey.Key, state);  //Funkystation - Swapped to set state instead of UI message
            }
            else if (seedComp.SeedId != null && _prototypeManager.TryIndex(seedComp.SeedId, out SeedPrototype? protoSeed))
            {
                var state = ObtainingGeneDataSeed(protoSeed, target, false, ent.Comp.Settings.AnalyzerModes);
                _uiSystem.SetUiState(ent.Owner, PlantAnalyzerUiKey.Key, state); //Funkystation - Swapped to set state instead of UI message
            }
        }
        else if (TryComp<PlantHolderComponent>(target, out var plantComp))
        {
            if (plantComp.Seed != null)
            {
                var state = ObtainingGeneDataSeed(plantComp.Seed, target, true, ent.Comp.Settings.AnalyzerModes);
                _uiSystem.SetUiState(ent.Owner, PlantAnalyzerUiKey.Key, state); //Funkystation - Swapped to set state instead of UI message
            }
        }
    }

    /// <summary>
    ///     Analysis of seed from prototype.
    /// </summary>
    public PlantAnalyzerScannedSeedPlantInformation ObtainingGeneDataSeed(SeedData seedData, EntityUid target, bool isTray, PlantAnalyzerModes scannerMode)
    {
        bool scanIsAdvanced = (scannerMode == PlantAnalyzerModes.AdvancedScan);
        // Get trickier fields first.
        AnalyzerHarvestType harvestType = (AnalyzerHarvestType) seedData.HarvestRepeat;

        var mutationProtos = seedData.MutationPrototypes;
        List<string> mutationStrings = new();
        foreach (var mutationProto in mutationProtos)
        {
            if (_prototypeManager.TryIndex<SeedPrototype>(mutationProto, out var seed))
            {
                mutationStrings.Add(seed.DisplayName);
            }
        }

        PlantAnalyzerScannedSeedPlantInformation ret = new()
        {
            TargetEntity = GetNetEntity(target),
            IsTray = isTray,
            SeedName = seedData.DisplayName,
            SeedChem = seedData.Chemicals.Keys.ToArray(),
            HarvestType = harvestType,
            ExudeGases = GetGasFlags(seedData.ExudeGasses.Keys),
            ConsumeGases = GetGasFlags(seedData.ConsumeGasses.Keys),
            Endurance = seedData.Endurance,
            SeedYield = seedData.Yield,
            Lifespan = seedData.Lifespan,
            Maturation = seedData.Maturation,
            Production = seedData.Production,
            GrowthStages = seedData.GrowthStages,
            SeedPotency = seedData.Potency,
            Speciation = mutationStrings.ToArray()
        };

        if (scanIsAdvanced)
        {
            AdvancedScanInfo advancedInfo = new()
            {
                NutrientConsumption = seedData.NutrientConsumption,
                WaterConsumption = seedData.WaterConsumption,
                IdealHeat = seedData.IdealHeat,
                HeatTolerance = seedData.HeatTolerance,
                IdealLight = seedData.IdealLight,
                LightTolerance = seedData.LightTolerance,
                ToxinsTolerance = seedData.ToxinsTolerance,
                LowPressureTolerance = seedData.LowPressureTolerance,
                HighPressureTolerance = seedData.HighPressureTolerance,
                PestTolerance = seedData.PestTolerance,
                WeedTolerance = seedData.WeedTolerance,
                Mutations = GetMutationFlags(seedData)
            };

            ret.AdvancedInfo = advancedInfo;
        }
        return ret;
    }

    public MutationFlags GetMutationFlags(SeedData plant)
    {
        MutationFlags ret = MutationFlags.None;
        if (plant.TurnIntoKudzu) ret |= MutationFlags.TurnIntoKudzu;
        if (plant.Seedless) ret |= MutationFlags.Seedless;
        if (plant.Ligneous) ret |= MutationFlags.Ligneous;
        if (plant.CanScream) ret |= MutationFlags.CanScream;

        return ret;
    }

    //Funkystation - Adjusted to work for new gases
    public string[] GetGasFlags(IEnumerable<Gas> gases)
    {
        int gasLength = gases.Count();
        string[] plantGases = new string[gasLength];
        int i = 0;
        foreach (var gas in gases)
        {
            // Funkystation - 
            // plantGases[i] = Atmospherics.GasNames.GetValueOrDefault(gas, Loc.GetString("gases-unknown"));
            plantGases[i] = Loc.GetString($"gases-{gas}");
            i++;
        }
        return plantGases;
    }

    private void OnModeSelected(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerSetMode args)
    {
        SetMode(ent, args.ScannerModes);
    }

    public void SetMode(Entity<PlantAnalyzerComponent> ent, PlantAnalyzerModes mode)
    {
        if (ent.Comp.DoAfter != null)
            return;
        ent.Comp.Settings.AnalyzerModes = mode;

        var state = new PlantAnalyzerCurrentMode(ent.Comp.Settings.AnalyzerModes);
        _uiSystem.SetUiState(ent.Owner, PlantAnalyzerUiKey.Key, state);

        SendCurrentIndex(ent);
    }

    private void OnGeneIterate(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerGeneIterate args)
    {
        GeneIterate(ent, args.MutationIterate, args.IsDatabank);
        SendCurrentIndex(ent);
    }

    private void SendCurrentIndex(Entity<PlantAnalyzerComponent> ent)
    {
        int currentCount = ent.Comp.GeneIndex;
        if (ent.Comp.Settings.AnalyzerModes == PlantAnalyzerModes.Implant)
        {
            currentCount = ent.Comp.DatabankIndex;
        }
        var state = new PlantAnalyzerCurrentCount(currentCount);
        _uiSystem.SetUiState(ent.Owner, PlantAnalyzerUiKey.Key, state);
    }

    public void GeneIterate(Entity<PlantAnalyzerComponent> ent, bool mode, bool isDatabank)
    {
        if (ent.Comp.DoAfter != null)
            return;
        if (isDatabank)
        {
            if (mode)
            {
                ent.Comp.DatabankIndex += 1;
                int intCount = ent.Comp.GeneBank.Count + ent.Comp.ConsumeGasesBank.Count + ent.Comp.ExudeGasesBank.Count + ent.Comp.ChemicalBank.Count;
                if (ent.Comp.DatabankIndex >= intCount)
                {
                    ent.Comp.DatabankIndex = intCount - 1;
                    if (ent.Comp.DatabankIndex < 0)
                    {
                        ent.Comp.DatabankIndex = 0;
                    }
                }
            }
            else
            {
                ent.Comp.DatabankIndex -= 1;
                if (ent.Comp.DatabankIndex < 0)
                {
                    ent.Comp.DatabankIndex = 0;
                }
            }
        }
        else
        {
            if (mode)
            {
                ent.Comp.GeneIndex += 1;
                if (ent.Comp.GeneIndex >= SeedDataTypes.IdToType.Count + 1 + 1 + 1)
                {
                    ent.Comp.GeneIndex = SeedDataTypes.IdToType.Count + 1 + 1 + 1 - 1;
                }
            }
            else
            {
                ent.Comp.GeneIndex -= 1;
                if (ent.Comp.GeneIndex < 0)
                {
                    ent.Comp.GeneIndex = 0;
                }
            }

        }
    }

    public void OnDeleteDatabaseEntry(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerDeleteDatabankEntry args)
    {
        if (args.IsDeleteMutations)
        {
            // implement later, need to get an actual seed lmao.
            return;
        }
        else
        {
            if (ent.Comp.GeneBank.Count + ent.Comp.ConsumeGasesBank.Count + ent.Comp.ExudeGasesBank.Count + ent.Comp.ChemicalBank.Count <= 0)
            {
                SendCurrentIndex(ent);
                return;
            }
            int intCount = 0;
            if (ent.Comp.DatabankIndex >= intCount + ent.Comp.GeneBank.Count)
            {
                intCount += ent.Comp.GeneBank.Count;
                if (ent.Comp.DatabankIndex >= intCount + ent.Comp.ConsumeGasesBank.Count)
                {
                    intCount += ent.Comp.ConsumeGasesBank.Count;
                    if (ent.Comp.DatabankIndex >= intCount + ent.Comp.ExudeGasesBank.Count)
                    {
                        intCount += ent.Comp.ExudeGasesBank.Count;
                        ent.Comp.ChemicalBank.RemoveAt(ent.Comp.DatabankIndex - intCount);
                    }
                    else
                    {
                        ent.Comp.ExudeGasesBank.RemoveAt(ent.Comp.DatabankIndex - intCount);
                    }
                }
                else
                {
                    ent.Comp.ConsumeGasesBank.RemoveAt(ent.Comp.DatabankIndex - intCount);
                }
            }
            else
            {
                ent.Comp.GeneBank.RemoveAt(ent.Comp.DatabankIndex);
            }
            intCount = ent.Comp.GeneBank.Count + ent.Comp.ConsumeGasesBank.Count + ent.Comp.ExudeGasesBank.Count + ent.Comp.ChemicalBank.Count;
            if (ent.Comp.DatabankIndex >= intCount)
            {
                ent.Comp.DatabankIndex = intCount - 1;
                if (ent.Comp.DatabankIndex < 0)
                {
                    ent.Comp.DatabankIndex = 0;
                }
            }
        }
        _uiSystem.SetUiState(ent.Owner, PlantAnalyzerUiKey.Key, new PlantAnalyzerSeedDatabank(ent.Comp.GeneBank, ent.Comp.ConsumeGasesBank, ent.Comp.ExudeGasesBank, ent.Comp.ChemicalBank));
    }

    // This is some shit which is really fucking wack.
    public void GetGeneFromInteger(Entity<PlantAnalyzerComponent> ent, SeedData seed)
    {
        int index = ent.Comp.GeneIndex;
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
                        ent.Comp.ChemicalBank.Add(new ChemData(chemical.Key, new SeedChemQuantityHelper(chemical.Value.Min, chemical.Value.Max, chemical.Value.PotencyDivisor, chemical.Value.Inherent)));
                    }
                }
                else
                {
                    foreach (KeyValuePair<Gas, float> gas in seed.ExudeGasses)
                    {
                        ent.Comp.ExudeGasesBank.Add(new GasData(gas.Key, gas.Value));
                    }
                }
            }
            else
            {
                foreach (KeyValuePair<Gas, float> gas in seed.ConsumeGasses)
                {
                    ent.Comp.ConsumeGasesBank.Add(new GasData(gas.Key, gas.Value));
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
            ent.Comp.GeneBank.Add(new GeneData(index, seedData[index]));
        }
    }

    public void SetGeneFromInteger(Entity<PlantAnalyzerComponent> ent, ref SeedData seed)
    {
        if (!seed.Unique)
        {
            seed = seed.Clone();
        }
        int index = ent.Comp.DatabankIndex;
        int intCount = 0;
        if (index >= intCount + ent.Comp.GeneBank.Count)
        {
            intCount += ent.Comp.GeneBank.Count;
            if (index >= intCount + ent.Comp.ConsumeGasesBank.Count)
            {
                intCount += ent.Comp.ConsumeGasesBank.Count;
                if (index >= intCount + ent.Comp.ExudeGasesBank.Count)
                {
                    intCount += ent.Comp.ExudeGasesBank.Count;
                    ChemData chem = ent.Comp.ChemicalBank[index - intCount];
                    SeedChemQuantity chemical = new SeedChemQuantity();
                    chemical.Min = chem.ChemValue.Min;
                    chemical.Max = chem.ChemValue.Max;
                    chemical.PotencyDivisor = chem.ChemValue.PotencyDivisor;
                    chemical.Inherent = chem.ChemValue.Inherent;
                    seed.Chemicals.Add(chem.ChemID, chemical);
                }
                else
                {
                    GasData gas = ent.Comp.ExudeGasesBank[index - intCount];
                    seed.ExudeGasses.Add(gas.GasID, gas.GasValue);
                }
            }
            else
            {
                GasData gas = ent.Comp.ConsumeGasesBank[index - intCount];
                seed.ConsumeGasses.Add(gas.GasID, gas.GasValue);
            }
        }
        else
        {
            GeneData gene = ent.Comp.GeneBank[index];
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

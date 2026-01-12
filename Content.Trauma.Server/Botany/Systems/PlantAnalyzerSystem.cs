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
using Content.Shared.Random;
using Content.Trauma.Common.Botany;
using Content.Trauma.Server.Botany.Components;
using Content.Trauma.Shared.Botany.Components;
using Content.Trauma.Shared.Botany.PlantAnalyzer;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.Botany.Systems;

public sealed class PlantAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerSetMode>(OnModeSelected);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerSetGeneIndex>(OnGeneIterate);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerDeleteDatabankEntry>(OnDeleteDatabaseEntry);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerRequestDefault>(OnRequestDefault);
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

            if (ent.Comp.Settings.AnalyzerModes == PlantAnalyzerModes.Scan)
            {
                var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.Settings.ScanDelay, new PlantAnalyzerDoAfterEvent(), ent, target: target, used: ent)
                {
                    NeedHand = true,
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    MovementThreshold = 0.01f
                };
                _doAfter.TryStartDoAfter(doAfterArgs, out ent.Comp.DoAfter);
            }
            else
            {
                var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.Settings.ModeDelay, new PlantAnalyzerDoAfterEvent(), ent, target: target, used: ent)
                {
                    NeedHand = true,
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    MovementThreshold = 0.01f
                };
                _doAfter.TryStartDoAfter(doAfterArgs, out ent.Comp.DoAfter);
            }
        }
    }

    private void OnDoAfter(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerDoAfterEvent args)
    {
        ent.Comp.DoAfter = null;
        if (args.Handled || args.Cancelled)
            return;
        // Double charge use for scan.
        if (ent.Comp.Settings.AnalyzerModes == PlantAnalyzerModes.Scan)
        {
            if (!_cell.TryUseActivatableCharge(ent.Owner, user: args.User))
                return;
        }
        if (args.Target is not { } target || !_cell.TryUseActivatableCharge(ent.Owner, user: args.User))
            return;

        if (ent.Comp.Settings.AnalyzerModes == PlantAnalyzerModes.Scan)
        {
            ReadScannedPlant(ent, target);
        }
        else if (ent.Comp.Settings.AnalyzerModes == PlantAnalyzerModes.DeleteMutations)
        {
            DeleteMutations(ent, target);
        }
        else if (ent.Comp.Settings.AnalyzerModes == PlantAnalyzerModes.Extract)
        {
            ExtractGene(ent, target);
        }
        else if (ent.Comp.Settings.AnalyzerModes == PlantAnalyzerModes.Implant)
        {
            InjectGene(ent, target);
        }
        _ui.TryOpenUi(args.User, PlantAnalyzerUiKey.Key, ent);
        args.Handled = true;
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
            else if (seedComp.SeedId != null && _proto.Resolve(seedComp.SeedId, out SeedPrototype? protoSeed))
            {
                // Copy genes to databank.
                GetGeneFromInteger(ent, protoSeed);
                // Delete seed
                Del(target);
            }
        }
        _audio.PlayPvs(ent.Comp.ExtractEndSound, ent);
        SendDatabase(ent);
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
                if (seedComp.SeedId == null || !_proto.Resolve(seedComp.SeedId, out SeedPrototype? protoSeed))
                    return;
                seedComp.Seed = protoSeed.Clone();
                SetGeneFromInteger(ent, ref seedComp.Seed);
            }
        }
        _audio.PlayPvs(ent.Comp.InjectEndSound, ent);
        SendDatabase(ent);
    }

    public void DeleteMutations(Entity<PlantAnalyzerComponent> ent, EntityUid target)
    {
        if (TryComp<SeedComponent>(target, out var seedComp))
        {
            if (seedComp.Seed != null)
            {
                seedComp.Seed.Mutations.Clear();
            }
            else
            {
                if (seedComp.SeedId == null || !_proto.Resolve(seedComp.SeedId, out SeedPrototype? protoSeed))
                    return;
                seedComp.Seed = protoSeed.Clone();
                seedComp.Seed.Mutations.Clear();
            }
        }
        _audio.PlayPvs(ent.Comp.DeleteMutationEndSound, ent);
    }
    public void ReadScannedPlant(Entity<PlantAnalyzerComponent> ent, EntityUid target)  //Funkystation - Renamed to match plants instead of copying HealthAnalyzer func names
    {

        if (TryComp<SeedComponent>(target, out var seedComp))
        {
            if (seedComp.Seed != null)
            {
                var state = ObtainingGeneDataSeed(ent, seedComp.Seed, target, false);
                _ui.SetUiState(ent.Owner, PlantAnalyzerUiKey.Key, state);  //Funkystation - Swapped to set state instead of UI message
            }
            else if (seedComp.SeedId != null && _proto.Resolve(seedComp.SeedId, out SeedPrototype? protoSeed))
            {
                var state = ObtainingGeneDataSeed(ent, protoSeed, target, false);
                _ui.SetUiState(ent.Owner, PlantAnalyzerUiKey.Key, state); //Funkystation - Swapped to set state instead of UI message
            }
        }
        else if (TryComp<PlantHolderComponent>(target, out var plantComp))
        {
            if (plantComp.Seed != null)
            {
                var state = ObtainingGeneDataSeed(ent, plantComp.Seed, target, true);
                _ui.SetUiState(ent.Owner, PlantAnalyzerUiKey.Key, state); //Funkystation - Swapped to set state instead of UI message
            }
        }

        _audio.PlayPvs(ent.Comp.ScanningEndSound, ent);
    }

    /// <summary>
    ///     Analysis of seed from prototype.
    /// </summary>
    public PlantAnalyzerScannedSeedPlantInformation ObtainingGeneDataSeed(Entity<PlantAnalyzerComponent> ent, SeedData seedData, EntityUid target, bool isTray)
    {
        // Get trickier fields first.
        HarvestType harvestType = seedData.HarvestRepeat;

        var mutationProtos = seedData.MutationPrototypes;
        ent.Comp.StoredMutationStrings.Clear();
        foreach (var mutationProto in mutationProtos)
        {
            if (_proto.Resolve<SeedPrototype>(mutationProto, out var seed))
            {
                ent.Comp.StoredMutationStrings.Add(seed.DisplayName);
            }
        }

        return new PlantAnalyzerScannedSeedPlantInformation
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
            Speciation = ent.Comp.StoredMutationStrings.ToArray(),
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

        if (ent.Comp.Settings.AnalyzerModes == PlantAnalyzerModes.Implant)
        {
            SendDatabase(ent);
        }
    }

    private void OnGeneIterate(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerSetGeneIndex args)
    {
        GeneIterate(ent, args.Index, args.IsDatabank);
        //SendCurrentIndex(ent);
    }

    private void SendCurrentIndex(Entity<PlantAnalyzerComponent> ent)
    {
        var state = new PlantAnalyzerCurrentCount(ent.Comp.GeneIndex, ent.Comp.DatabankIndex);
        _ui.SetUiState(ent.Owner, PlantAnalyzerUiKey.Key, state);
    }

    public void GeneIterate(Entity<PlantAnalyzerComponent> ent, int mode, bool isDatabank)
    {
        if (ent.Comp.DoAfter != null)
            return;
        if (isDatabank)
        {
            ent.Comp.DatabankIndex = mode;
            int intCount = ent.Comp.GeneBank.Count + ent.Comp.ConsumeGasesBank.Count + ent.Comp.ExudeGasesBank.Count + ent.Comp.ChemicalBank.Count + ent.Comp.MutationBank.Count;
            if (ent.Comp.DatabankIndex >= intCount)
            {
                ent.Comp.DatabankIndex = intCount - 1;
            }
            if (ent.Comp.DatabankIndex < 0)
            {
                ent.Comp.DatabankIndex = 0;
            }
        }
        else
        {
            ent.Comp.GeneIndex = mode;
            if (ent.Comp.GeneIndex >= SeedDataTypes.IdToType.Count)
            {
                ent.Comp.GeneIndex = SeedDataTypes.IdToType.Count - 1;
            }
            if (ent.Comp.GeneIndex < 0)
            {
                ent.Comp.GeneIndex = 0;
            }
        }
    }

    public void OnDeleteDatabaseEntry(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerDeleteDatabankEntry args)
    {
        int totalCount = ent.Comp.GeneBank.Count + ent.Comp.ConsumeGasesBank.Count + ent.Comp.ExudeGasesBank.Count + ent.Comp.ChemicalBank.Count;
        if (totalCount <= 0 || totalCount <= ent.Comp.DatabankIndex)
        {
            //SendCurrentIndex(ent);
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
        SendDatabase(ent);
    }

    public void OnRequestDefault(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerRequestDefault args)
    {
        var state = new PlantAnalyzerCurrentMode(ent.Comp.Settings.AnalyzerModes);
        _ui.SetUiState(ent.Owner, PlantAnalyzerUiKey.Key, state);
    }
    public void SendDatabase(Entity<PlantAnalyzerComponent> ent)
    {
        _ui.SetUiState(ent.Owner, PlantAnalyzerUiKey.Key, new PlantAnalyzerSeedDatabank(ent.Comp.GeneBank, ent.Comp.ConsumeGasesBank, ent.Comp.ExudeGasesBank, ent.Comp.ChemicalBank, ent.Comp.MutationBank, ent.Comp.GeneIndex, ent.Comp.DatabankIndex));
    }
    // This is some shit which is really fucking wack.
    public void GetGeneFromInteger(Entity<PlantAnalyzerComponent> ent, SeedData seed)
    {
        int index = ent.Comp.GeneIndex;
        if (index < 0)
        {
            return;
        }

        switch (SeedDataTypes.IdToType[index])
        {
            case SeedDataTypes.SeedDataType.RandomPlantMutation:
                foreach (var mutation in seed.Mutations)
                {
                    ent.Comp.MutationBank.Add(mutation);
                }
                break;
            case SeedDataTypes.SeedDataType.Chemical:
                foreach (var chemical in seed.Chemicals)
                {
                    ent.Comp.ChemicalBank.Add(new ChemData(chemical.Key, chemical.Value));
                }
                break;
            case SeedDataTypes.SeedDataType.GasConsume:
                foreach (var gas in seed.ConsumeGasses)
                {
                    ent.Comp.ConsumeGasesBank.Add(new GasData(gas.Key, gas.Value));
                }
                break;
            case SeedDataTypes.SeedDataType.GasExude:
                foreach (var gas in seed.ExudeGasses)
                {
                    ent.Comp.ExudeGasesBank.Add(new GasData(gas.Key, gas.Value));
                }
                break;
            default:
                {
                    float value = index switch
                    {
                        0 => seed.NutrientConsumption,
                        1 => seed.WaterConsumption,
                        2 => seed.IdealHeat,
                        3 => seed.HeatTolerance,
                        4 => seed.IdealLight,
                        5 => seed.LightTolerance,
                        6 => seed.ToxinsTolerance,
                        7 => seed.LowPressureTolerance,
                        8 => seed.HighPressureTolerance,
                        9 => seed.PestTolerance,
                        10 => seed.WeedTolerance,
                        11 => seed.Endurance,
                        12 => (float) seed.Yield,
                        13 => seed.Lifespan,
                        14 => seed.Maturation,
                        15 => seed.Production,
                        16 => (float) seed.HarvestRepeat,
                        17 => seed.Potency,
                        18 => (float) Convert.ToInt16(seed.Seedless),
                        19 => (float) Convert.ToInt16(seed.Viable),
                        20 => (float) Convert.ToInt16(seed.Ligneous),
                        21 => (float) Convert.ToInt16(seed.CanScream),
                        22 => (float) Convert.ToInt16(seed.TurnIntoKudzu),
                        _ => 0f
                    };
                    ent.Comp.GeneBank.Add(new GeneData(index, value));
                    break;
                }
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
                    if (index >= intCount + ent.Comp.ChemicalBank.Count)
                    {
                        seed.Mutations.Add(ent.Comp.MutationBank[index - intCount]);
                    }
                    else
                    {
                        ChemData chem = ent.Comp.ChemicalBank[index - intCount];
                        SeedChemQuantity chemical = new SeedChemQuantity();
                        chemical.Min = chem.ChemValue.Min;
                        chemical.Max = chem.ChemValue.Max;
                        chemical.PotencyDivisor = chem.ChemValue.PotencyDivisor;
                        chemical.Inherent = chem.ChemValue.Inherent;
                        seed.Chemicals[chem.ChemID] = chemical;
                    }
                }
                else
                {
                    GasData gas = ent.Comp.ExudeGasesBank[index - intCount];
                    seed.ExudeGasses[gas.GasID] = gas.GasValue;
                }
            }
            else
            {
                GasData gas = ent.Comp.ConsumeGasesBank[index - intCount];
                seed.ConsumeGasses[gas.GasID] = gas.GasValue;
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
                        seed.HarvestRepeat = (HarvestType) gene.GeneValue;
                        break;
                    }
                case 17:
                    {
                        seed.Potency = gene.GeneValue;
                        break;
                    }
                case 18:
                    {
                        seed.Seedless = Convert.ToBoolean(gene.GeneValue);
                        break;
                    }
                case 19:
                    {
                        seed.Viable = Convert.ToBoolean(gene.GeneValue);
                        break;
                    }
                case 20:
                    {
                        seed.Ligneous = Convert.ToBoolean(gene.GeneValue);
                        break;
                    }
                case 21:
                    {
                        seed.CanScream = Convert.ToBoolean(gene.GeneValue);
                        break;
                    }
                case 22:
                    {
                        seed.TurnIntoKudzu = Convert.ToBoolean(gene.GeneValue);
                        break;
                    }
            }
        }
    }
}

// SPDX-FileCopyrightText: 2025 Liamofthesky <157073227+Liamofthesky@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 ReconPangolin <67752926+ReconPangolin@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using System.Linq;
using System.Text;
using Content.Server.Botany.Components;
using Content.Server.Construction.Completions;
using Content.Shared._NF.PlantAnalyzer;
using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using JetBrains.FormatRipper.Elf;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using static Content.Server.Botany.Components.PlantAnalyzerComponent;

namespace Content.Server.Botany.Systems;

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
        if (args.Target == null || !args.CanReach || !_cell.HasActivatableCharge(ent.Owner, user: args.User))
            return;

        if (ent.Comp.DoAfter != null)
            return;

        if (HasComp<SeedComponent>(args.Target) || TryComp<PlantHolderComponent>(args.Target, out var plantHolder) && plantHolder.Seed != null)
        {

            if (ent.Comp.Settings.AnalyzerModes == PlantAnalyzerModes.AdvancedScan)
            {
                var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.Settings.AdvScanDelay, new PlantAnalyzerDoAfterEvent(), ent, target: args.Target, used: ent)
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
                var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.Settings.ScanDelay, new PlantAnalyzerDoAfterEvent(), ent, target: args.Target, used: ent)
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
                ent.Comp.GetGeneFromInteger(ent.Comp.GeneIndex, seedComp.Seed);
                // Delete seed
                EntityManager.DeleteEntity(target);
            }
            else if (seedComp.SeedId != null && _prototypeManager.TryIndex(seedComp.SeedId, out SeedPrototype? protoSeed))
            {
                // Copy genes to databank.
                ent.Comp.GetGeneFromInteger(ent.Comp.GeneIndex, protoSeed);
                // Delete seed
                EntityManager.DeleteEntity(target);
            }
        }
        else if (TryComp<PlantHolderComponent>(target, out var plantComp))
        {
            if (plantComp.Seed != null)
            {
                // Copy genes to databank.
                ent.Comp.GetGeneFromInteger(ent.Comp.GeneIndex, plantComp.Seed);
                // EntityManager.DeleteEntity(target);
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
                ent.Comp.SetGeneFromInteger(ent.Comp.DatabankIndex, seedComp.Seed);
            }
            else if (seedComp.SeedId != null && _prototypeManager.TryIndex(seedComp.SeedId, out SeedPrototype? protoSeed))
            {
                ent.Comp.SetGeneFromInteger(ent.Comp.DatabankIndex, protoSeed);
            }
        }
        else if (TryComp<PlantHolderComponent>(target, out var plantComp))
        {
            if (plantComp.Seed != null)
            {
                ent.Comp.SetGeneFromInteger(ent.Comp.DatabankIndex, plantComp.Seed);
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
        AnalyzerHarvestType harvestType = AnalyzerHarvestType.Unknown;
        switch (seedData.HarvestRepeat)
        {
            case HarvestType.Repeat:
                harvestType = AnalyzerHarvestType.Repeat;
                break;
            case HarvestType.NoRepeat:
                harvestType = AnalyzerHarvestType.NoRepeat;
                break;
            case HarvestType.SelfHarvest:
                harvestType = AnalyzerHarvestType.SelfHarvest;
                break;
            default:
                break;
        }

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
            // Need to actually localize this shit like funky does.
            plantGases[i] = gas.ToString();
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
}

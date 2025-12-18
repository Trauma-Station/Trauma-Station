// SPDX-FileCopyrightText: 2025 Liamofthesky <157073227+Liamofthesky@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 ReconPangolin <67752926+ReconPangolin@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Shared.Random;
using Content.Trauma.Common.Botany;
using Content.Trauma.Shared.Botany.Components;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Botany.PlantAnalyzer;

/// <summary>
///     The information about the last scanned plant/seed is stored here.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlantAnalyzerScannedSeedPlantInformation : BoundUserInterfaceState //Funkystation - Swapped to set BoundUserInterfaceState instead of BoundUserInterfaceMessage
{
    public NetEntity? TargetEntity;
    public bool IsTray;

    public string? SeedName;
    public string[]? SeedChem;
    public HarvestType HarvestType;
    public string[]? ExudeGases; //Funkystation - Swapped to string
    public string[]? ConsumeGases; //Funkystation - Swapped to string
    public float Endurance;
    public int SeedYield;
    public float Lifespan;
    public float Maturation;
    public float Production;
    public int GrowthStages;
    public float SeedPotency;
    public string[]? Speciation; // Currently only available on server, we need to send strings to the client.
    public float NutrientConsumption;
    public float WaterConsumption;
    public float IdealHeat;
    public float HeatTolerance;
    public float IdealLight;
    public float LightTolerance;
    public float ToxinsTolerance;
    public float LowPressureTolerance;
    public float HighPressureTolerance;
    public float PestTolerance;
    public float WeedTolerance;
    public MutationFlags Mutations;
}

/// <summary>
///     Information gathered in an advanced scan.
/// </summary>
[Serializable, NetSerializable]
public struct AdvancedScanInfo
{
    public float NutrientConsumption;
    public float WaterConsumption;
    public float IdealHeat;
    public float HeatTolerance;
    public float IdealLight;
    public float LightTolerance;
    public float ToxinsTolerance;
    public float LowPressureTolerance;
    public float HighPressureTolerance;
    public float PestTolerance;
    public float WeedTolerance;
    public MutationFlags Mutations;
}

// Note: currently leaving out Viable.
[Flags]
public enum MutationFlags : byte
{
    None = 0,
    TurnIntoKudzu = 1,
    Seedless = 2,
    Slip = 4,
    Sentient = 8,
    Ligneous = 16,
    Bioluminescent = 32,
    CanScream = 64,
}

[Flags]
public enum GasFlags : short
{
    None = 0,
    Nitrogen = 1,
    Oxygen = 2,
    CarbonDioxide = 4,
    Plasma = 8,
    Tritium = 16,
    WaterVapor = 32,
    Ammonia = 64,
    NitrousOxide = 128,
    Frezon = 256,
}

[Serializable, NetSerializable]
public sealed class PlantAnalyzerSetMode(PlantAnalyzerModes scannerModes) : BoundUserInterfaceMessage
{
    public PlantAnalyzerModes ScannerModes { get; } = scannerModes;
}

[Serializable, NetSerializable]
public sealed class PlantAnalyzerGeneIterate(bool up, bool isDatabank) : BoundUserInterfaceMessage
{
    public bool MutationIterate { get; } = up;
    public bool IsDatabank { get; } = isDatabank;
}

[Serializable, NetSerializable]
public sealed class PlantAnalyzerCurrentMode(PlantAnalyzerModes currentMode) : BoundUserInterfaceState
{
    public PlantAnalyzerModes CurrentMode { get; } = currentMode;
}

[Serializable, NetSerializable]
public sealed class PlantAnalyzerCurrentCount(int geneIndex, int databaseIndex) : BoundUserInterfaceState
{
    public int GeneIndex { get; } = geneIndex;
    public int DatabaseIndex { get; } = databaseIndex;
}

[Serializable, NetSerializable]
public sealed class PlantAnalyzerSeedDatabank(List<GeneData> seedData, List<GasData> consumeGasData, List<GasData> exudeGasData, List<ChemData> chemicalData, List<RandomPlantMutation> mutationData, int geneIndex, int databaseIndex) : BoundUserInterfaceState
{
    public List<GeneData> SeedData { get; } = seedData;
    public List<GasData> ConsumeGasData { get; } = consumeGasData;
    public List<GasData> ExudeGasData { get; } = exudeGasData;
    public List<ChemData> ChemicalData { get; } = chemicalData;
    public List<RandomPlantMutation> MutationData { get; } = mutationData;
    public int GeneIndex { get; } = geneIndex;
    public int DatabaseIndex { get; } = databaseIndex;
}


[Serializable, NetSerializable]
public sealed class PlantAnalyzerDeleteDatabankEntry : BoundUserInterfaceMessage;


[Serializable, NetSerializable]
public sealed class PlantAnalyzerRequestDefault : BoundUserInterfaceMessage;

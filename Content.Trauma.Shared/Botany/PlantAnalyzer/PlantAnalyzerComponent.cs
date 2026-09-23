// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Botany.PlantAnalyzer;

/// <summary>
/// Allows viewing data from plants/seeds and modifying a seed's data.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class PlantAnalyzerComponent : Component
{
    [DataField, AutoNetworkedField]
    public PlantAnalyzerModes Mode = PlantAnalyzerModes.Scan;

    [DataField(required: true)]
    public TimeSpan ScanDelay;

    [DataField(required: true)]
    public TimeSpan ModeDelay;

    [DataField, AutoNetworkedField]
    public bool Busy;

    /// <summary>
    /// The scanned tray, plant or seed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Scanned;

    /// <summary>
    /// The plant data being scanned, null if scanning a baseline seed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Plant;

    /// <summary>
    /// The seed's plant prototype if scanning a seed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? Seed;

    /// <summary>
    /// Snapshot of the mutations present when a plant was last scanned.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> ScannedMutations = new();

    [DataField]
    public SoundSpecifier? ScanningEndSound;

    [DataField]
    public SoundSpecifier? DeleteMutationEndSound;

    [DataField]
    public SoundSpecifier? ExtractEndSound;

    [DataField]
    public SoundSpecifier? InjectEndSound;

    [DataField, AutoNetworkedField]
    public List<GeneData> GeneBank = new();

    [DataField, AutoNetworkedField]
    public List<GasData> ConsumeGasesBank = new();

    [DataField, AutoNetworkedField]
    public List<GasData> ExudeGasesBank = new();

    [DataField, AutoNetworkedField]
    public List<ChemData> ChemicalBank = new();

    [DataField, AutoNetworkedField]
    public int GeneIndex = 0;

    [DataField, AutoNetworkedField]
    public int DatabankIndex = 0;
}

// has to match the UI's tab order
[Serializable, NetSerializable]
public enum PlantAnalyzerModes : byte
{
    Scan,
    DeleteMutations,
    Extract,
    Implant
}

[Serializable, NetSerializable]
public partial record struct GeneData(int GeneID, float GeneValue);

[Serializable, NetSerializable]
public partial record struct ChemData(string ChemID, PlantChemQuantity ChemValue);

[Serializable, NetSerializable]
public partial record struct GasData(Gas GasID, float GasValue);

public enum SeedDataType : byte
{
    Float,
    Int,
    HarvestType,
    Bool,
    GasConsume,
    GasExude,
    Chemical
}

// This is some shit which is really fucking wack.
public record struct SeedData(SeedDataType Type, string Name)
{
    public static readonly SeedData[] AllGenes =
    [
        new(SeedDataType.Float, "NutrientConsumption"), // 0
        new(SeedDataType.Float, "WaterConsumption"), // 1
        new(SeedDataType.Float, "ToxinsTolerance"), // 2
        new(SeedDataType.Float, "ToxinUptakeDivisor"), // 3
        new(SeedDataType.Float, "LowHeatTolerance"), // 4
        new(SeedDataType.Float, "HighHeatTolerance"), // 5
        new(SeedDataType.Float, "LowPressureTolerance"), // 6
        new(SeedDataType.Float, "HighPressureTolerance"), // 7
        new(SeedDataType.Float, "PestTolerance"), // 8
        new(SeedDataType.Float, "WeedTolerance"), // 9
        new(SeedDataType.Float, "Endurance"), // 10
        new(SeedDataType.Float, "Lifespan"), // 11
        new(SeedDataType.Float, "Maturation"), // 12
        new(SeedDataType.Float, "Production"), // 13
        new(SeedDataType.HarvestType, "HarvestType"), // 14
        new(SeedDataType.Int, "Yield"), // 15
        new(SeedDataType.Float, "Potency"), // 16
        new(SeedDataType.Bool, "Seedless"), // 17
        new(SeedDataType.Bool, "Unviable"), // 18
        new(SeedDataType.Bool, "Ligneous"), // 19
        new(SeedDataType.Bool, "CanScream"), // 20
        new(SeedDataType.Bool, "TurnIntoKudzu"), // 21
        new(SeedDataType.GasConsume, "Consume Gases"), // 22
        new(SeedDataType.GasExude, "Exude Gases"), // 23
        new(SeedDataType.Chemical, "Chemicals") // 24
    ];
}

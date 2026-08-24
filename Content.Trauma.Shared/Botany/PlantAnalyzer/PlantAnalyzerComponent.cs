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

    [DataField, AutoNetworkedField]
    public EntityUid? Plant;

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
        new(SeedDataType.Float, "NutrientConsumption"),
        new(SeedDataType.Float, "WaterConsumption"),
        new(SeedDataType.Float, "ToxinsTolerance"),
        new(SeedDataType.Float, "ToxinUptakeDivisor"),
        new(SeedDataType.Float, "LowHeatTolerance"),
        new(SeedDataType.Float, "HighHeatTolerance"),
        new(SeedDataType.Float, "LowPressureTolerance"),
        new(SeedDataType.Float, "HighPressureTolerance"),
        new(SeedDataType.Float, "PestTolerance"),
        new(SeedDataType.Float, "WeedTolerance"),
        new(SeedDataType.Float, "Endurance"),
        new(SeedDataType.Float, "Lifespan"),
        new(SeedDataType.Float, "Maturation"),
        new(SeedDataType.Float, "Production"),
        new(SeedDataType.HarvestType, "HarvestType"),
        new(SeedDataType.Int, "Yield"),
        new(SeedDataType.Float, "Potency"),
        new(SeedDataType.Bool, "Seedless"),
        new(SeedDataType.Bool, "Ligneous"),
        new(SeedDataType.Bool, "CanScream"),
        new(SeedDataType.Bool, "TurnIntoKudzu"),
        new(SeedDataType.GasConsume, "Consume Gases"),
        new(SeedDataType.GasExude, "Exude Gases"),
        new(SeedDataType.Chemical, "Chemicals")
    ];
}

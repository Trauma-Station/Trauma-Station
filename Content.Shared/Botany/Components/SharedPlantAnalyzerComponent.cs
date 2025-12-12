using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

using Content.Shared.Atmos;

namespace Content.Shared.Botany.Components;

public enum PlantAnalyzerModes
{
    BasicScan,
    AdvancedScan,
    Extract,
    Implant
}

[Serializable, NetSerializable]
public partial struct GeneData
{
    public int GeneID;

    public float GeneValue;

    public GeneData(int id, float value)
    {
        GeneID = id;
        GeneValue = value;
    }
}

[Serializable, NetSerializable]
public partial struct ChemData
{
    public string ChemID;

    public SeedChemQuantityAlternate ChemValue;

    public ChemData(string id, SeedChemQuantityAlternate value)
    {
        ChemID = id;
        ChemValue = value;
    }
}

[Serializable, NetSerializable]
public partial struct GasData
{
    public Gas GasID;

    public float GasValue;

    public GasData(Gas gasId, float value)
    {
        GasID = gasId;
        GasValue = value;
    }
}

// This is some shit which is really fucking wack.
// 0 - float, 1 - int, 2 - Enum HarvestType, 3 - bool
public partial struct SeedDataTypes
{
    // 0 - float, 1 - int, 2 - Enum HarvestType, 3 - bool, 4 - Gas, 5 - Chemical, 6 - class RandomPlantMutation
    public static readonly Dictionary<int, int> IdToType = new()
    {
        { 0, 0 },
        { 1, 0 },
        { 2, 0 },
        { 3, 0 },
        { 4, 0 },
        { 5, 0 },
        { 6, 0 },
        { 7, 0 },
        { 8, 0 },
        { 9, 0 },
        { 10, 0 },
        { 11, 0 },
        { 12, 1 },
        { 13, 0 },
        { 14, 0 },
        { 15, 0 },
        { 16, 1 },
        { 17, 2 },
        { 18, 0 },
        { 19, 3 },
        { 20, 3 },
        { 21, 3 },
        { 22, 3 },
        { 23, 3 }
    };

    public static readonly Dictionary<int, String> IdToString = new()
    {
        { 0, "NutrientConsumption" },
        { 1, "WaterConsumption" },
        { 2, "IdealHeat" },
        { 3, "HeatTolerance" },
        { 4, "IdealLight" },
        { 5, "LightTolerance" },
        { 6, "ToxinsTolerance" },
        { 7, "LowPressureTolerance" },
        { 8, "HighPressureTolerance" },
        { 9, "PestTolerance" },
        { 10, "WeedTolerance" },
        { 11, "Endurance" },
        { 12, "Yield" },
        { 13, "Lifespan" },
        { 14, "Maturation" },
        { 15, "Production" },
        { 16, "GrowthStages" },
        { 17, "HarvestRepeat" },
        { 18, "Potency" },
        { 19, "Seedless" },
        { 20, "Viable" },
        { 21, "Ligneous" },
        { 22, "CanScream" },
        { 23, "TurnIntoKudzu" },
        { 24, "Consume Gases" },
        { 25, "Exude Gases" },
        { 26, "Chemicals"}
    };
}

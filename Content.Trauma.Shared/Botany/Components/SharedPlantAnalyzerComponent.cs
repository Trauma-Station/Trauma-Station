using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Botany.Components;

[Serializable, NetSerializable]
public partial struct SeedChemQuantityHelper
{
    /// <summary>
    /// Minimum amount of chemical that is added to produce, regardless of the potency
    /// </summary>
    [DataField] public int Min = 0;

    /// <summary>
    /// Maximum amount of chemical that can be produced after taking plant potency into account.
    /// </summary>
    [DataField] public int Max = 5;

    /// <summary>
    /// When chemicals are added to produce, the potency of the seed is divided with this value. Final chemical amount is the result plus the `Min` value.
    /// Example: PotencyDivisor of 20 with seed potency of 55 results in 2.75, 55/20 = 2.75. If minimum is 1 then final result will be 3.75 of that chemical, 55/20+1 = 3.75.
    /// </summary>
    [DataField] public int PotencyDivisor = 20;

    /// <summary>
    /// Inherent chemical is one that is NOT result of mutation or crossbreeding. These chemicals are removed if species mutation is executed.
    /// </summary>
    [DataField] public bool Inherent = true;

    public SeedChemQuantityHelper(int min, int max, int potencyDivisor, bool inherent)
    {
        Min = min;
        Max = max;
        PotencyDivisor = potencyDivisor;
        Inherent = inherent;
    }
}


public enum PlantAnalyzerModes
{
    Scan,
    Extract,
    Implant,
    DeleteMutations
}

[Serializable, NetSerializable]
public partial record struct GeneData(int GeneID, float GeneValue);

[Serializable, NetSerializable]
public partial record struct ChemData(string ChemID, SeedChemQuantityHelper ChemValue);

[Serializable, NetSerializable]
public partial record struct GasData(Gas GasID, float GasValue);

// This is some shit which is really fucking wack.
// 0 - float, 1 - int, 2 - Enum HarvestType, 3 - bool
public partial struct SeedDataTypes
{
    public enum SeedDataType
    {
        Float,
        Int,
        HarvestType,
        Bool,
        GasConsume,
        GasExude,
        Chemical,
        RandomPlantMutation
    }

    // 0 - float, 1 - int, 2 - Enum HarvestType, 3 - bool, 4 - Gas, 5 - Chemical, 6 - class RandomPlantMutation
    public static readonly List<SeedDataType> IdToType = new()
    {
        SeedDataType.Float,
        SeedDataType.Float,
        SeedDataType.Float,
        SeedDataType.Float,
        SeedDataType.Float,
        SeedDataType.Float,
        SeedDataType.Float,
        SeedDataType.Float,
        SeedDataType.Float,
        SeedDataType.Float,
        SeedDataType.Float,
        SeedDataType.Float,
        SeedDataType.Int,
        SeedDataType.Float,
        SeedDataType.Float,
        SeedDataType.Float,
        SeedDataType.HarvestType,
        SeedDataType.Float,
        SeedDataType.Bool,
        SeedDataType.Bool,
        SeedDataType.Bool,
        SeedDataType.Bool,
        SeedDataType.Bool,
        SeedDataType.GasConsume,
        SeedDataType.GasExude,
        SeedDataType.Chemical,
        SeedDataType.RandomPlantMutation
    };

    public static readonly List<string> IdToString = new()
    {
        "NutrientConsumption",
        "WaterConsumption",
        "IdealHeat",
        "HeatTolerance",
        "IdealLight",
        "LightTolerance",
        "ToxinsTolerance",
        "LowPressureTolerance",
        "HighPressureTolerance",
        "PestTolerance",
        "WeedTolerance",
        "Endurance",
        "Yield",
        "Lifespan",
        "Maturation",
        "Production",
        "HarvestType",
        "Potency",
        "Seedless",
        "Viable",
        "Ligneous",
        "CanScream",
        "TurnIntoKudzu",
        "Consume Gases",
        "Exude Gases",
        "Chemical",
        "Mutations"
    };
}

// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.FixedPoint;
using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Content.Trauma.Common.Botany;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Botany.Components;

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
public partial record struct ChemData(string ChemID, SeedChemQuantity ChemValue);

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

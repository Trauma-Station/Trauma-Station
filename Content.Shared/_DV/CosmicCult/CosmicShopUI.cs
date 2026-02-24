using Content.Shared._DV.CosmicCult.Components;
using Content.Shared._DV.CosmicCult.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._DV.CosmicCult;

[Serializable, NetSerializable]
public enum CosmicShopKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CosmicShopBuiState(CosmicCultComponent comp) : BoundUserInterfaceState
{
    public HashSet<ProtoId<InfluencePrototype>> UnlockedInfluences = comp.UnlockedInfluences;
    public HashSet<ProtoId<InfluencePrototype>> OwnedInfluences = comp.OwnedInfluences;
    public int EntropyBudget = comp.EntropyBudget;
    public int CultistsForNextLevel = comp.CultistsForNextLevel;
    public bool LevelUpAwaitingConfirmation = comp.LevelUpAwaitingConfirmation;
    public float CurrentProgress = comp.TotalEntropy - comp.EntropyRequirementOffset;
    public float TargetProgress = comp.EntropyForNextLevel;
}

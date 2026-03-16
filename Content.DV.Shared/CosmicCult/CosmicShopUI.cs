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
public sealed class CosmicShopBuiState() : BoundUserInterfaceState
{
}

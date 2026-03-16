using Content.DV.Shared.CosmicCult.Components;
using Content.DV.Shared.CosmicCult.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.DV.Shared.CosmicCult;

[Serializable, NetSerializable]
public enum CosmicShopKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CosmicShopBuiState() : BoundUserInterfaceState
{
}

using Content.Trauma.Shared.CosmicCult.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.CosmicCult;

[Serializable, NetSerializable]
public enum CosmicShopKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CosmicShopBuiState() : BoundUserInterfaceState
{
}

using Content.Shared._DV.CosmicCult.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._DV.CosmicCult.Components;

/// <summary>
/// Component for the coscult power UI. Applied to the action itself because it's easier that way.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicShopComponent : Component
{
    [DataField] public SoundSpecifier PurchaseSfx = new SoundPathSpecifier("/Audio/_DV/CosmicCult/insert_entropy.ogg");
}

[Serializable, NetSerializable]
public sealed class InfluenceSelectedMessage(ProtoId<InfluencePrototype> influenceProtoId) : BoundUserInterfaceMessage
{
    public ProtoId<InfluencePrototype> InfluenceProtoId = influenceProtoId;
}

[Serializable, NetSerializable]
public sealed class LevelUpconfirmedMessage() : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RespecConfirmedMessage() : BoundUserInterfaceMessage;

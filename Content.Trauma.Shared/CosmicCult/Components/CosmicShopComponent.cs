// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.CosmicCult.Prototypes;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.CosmicCult.Components;

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

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;

[RegisterComponent, NetworkedComponent]
public sealed partial class MansusInfusedComponent : Component
{
    [DataField]
    public int MaxCharges = 1;

    [DataField]
    public int AvailableCharges = 1;

    [DataField]
    public string HeldPrefix = "infused";

    [DataField]
    public ProtoId<EntityEffectPrototype> InfusedHitEffect = "HereticBladeGraspEffect";
}

[Serializable, NetSerializable]
public enum InfusedBladeVisuals
{
    Infused,
}

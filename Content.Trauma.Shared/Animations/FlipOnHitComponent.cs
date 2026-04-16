// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Animations;

[RegisterComponent, NetworkedComponent]
public sealed partial class FlipOnHitComponent : Component
{
    [DataField]
    public bool LightAttackOnly;

    [DataField]
    public bool TriggerOnSelfHit;

    [DataField]
    public EntityWhitelist? HitWhitelist;

    [DataField]
    public EntProtoId StatusEffect = "StatusEffectFlip";
}

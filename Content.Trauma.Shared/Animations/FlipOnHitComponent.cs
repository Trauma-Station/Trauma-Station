// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Animations;

[RegisterComponent, NetworkedComponent]
public sealed partial class FlipOnHitComponent : Component
{
    [DataField]
    public bool LeftClickOnly;

    [DataField]
    public EntProtoId StatusEffect = "StatusEffectFlip";
}

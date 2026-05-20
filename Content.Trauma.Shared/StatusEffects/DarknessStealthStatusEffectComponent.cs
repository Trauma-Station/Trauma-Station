// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.StatusEffects;

[RegisterComponent, NetworkedComponent]
public sealed partial class DarknessStealthStatusEffectComponent : Component
{
    /// <summary>
    /// Light level to trigger cloak of darkness
    /// </summary>
    [DataField]
    public float TriggerAt = 0.25f;

    /// <summary>
    /// The visibility of the stealth.
    /// </summary>
    [DataField]
    public float Visibility = 0.15f;
}

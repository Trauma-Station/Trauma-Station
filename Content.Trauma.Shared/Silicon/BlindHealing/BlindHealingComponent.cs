// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Silicon.BlindHealing;

[RegisterComponent, NetworkedComponent]
public sealed partial class BlindHealingComponent : Component
{
    [DataField]
    public TimeSpan DoAfterDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     A multiplier that will be applied to the above if an entity is repairing themselves.
    /// </summary>
    [DataField]
    public float SelfHealPenalty = 3f;

    /// <summary>
    ///     Whether or not an entity is allowed to repair itself.
    /// </summary>
    [DataField]
    public bool AllowSelfHeal = true;

    /// <summary>
    /// Whitelist entities must match
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = default!;
}

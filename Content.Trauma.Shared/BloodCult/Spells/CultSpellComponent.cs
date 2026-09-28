// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Spells;

/// <summary>
/// Action component for all cult spells.
/// Requires that you can speak for actions to be performed.
/// Prevents using them on mindshielded targets by default.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CultSpellComponent : Component
{
    /// <summary>
    /// If true will ignore mindshield protection.
    /// </summary>
    [DataField]
    public bool BypassProtection;
}

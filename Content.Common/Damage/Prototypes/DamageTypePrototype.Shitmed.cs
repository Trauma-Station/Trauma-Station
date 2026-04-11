// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;

namespace Content.Shared.Damage.Prototypes;

public sealed partial class DamageTypePrototype
{
    /// <summary>
    /// Shitmed Change: Wounds with the said damage type will be having this multiplier
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 WoundHealingMultiplier { get; set; } = 1;
}

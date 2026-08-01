// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Emag;

/// <summary>
/// Makes AutoRechargeComponent recharge faster upon being emagged
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ModifyRechargeTimeOnEmagComponent : Component
{
    [DataField]
    public float Multiplier = 0.5f;
}

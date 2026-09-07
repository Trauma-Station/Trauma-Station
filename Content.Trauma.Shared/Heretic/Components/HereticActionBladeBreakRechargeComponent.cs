// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Heretic.Components;

/// <summary>
/// Action with <see cref="LimitedChargesComponent"/> that is recharged by breaking a blade as heretic
/// Should be added to t3 abilities only that are unlocked after heretic loses ability to break blades
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HereticActionBladeBreakRechargeComponent : Component
{
    [DataField]
    public int ChargeGain = 1;
}

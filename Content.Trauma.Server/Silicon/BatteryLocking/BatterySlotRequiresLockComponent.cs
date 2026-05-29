// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Silicons.BatteryLocking;

[RegisterComponent]
public sealed partial class BatterySlotRequiresLockComponent : Component
{
    [DataField]
    public string ItemSlot = string.Empty;
}

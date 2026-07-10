// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;

namespace Content.Trauma.Common.Strip;

/// <summary>
/// Raised on a user to check whether they're currently a stealthy thief, either directly
/// (ex: thief antag ThievingComponent on the mob itself) or via equipped gloves (relayed).
/// </summary>
[ByRefEvent]
public struct ThievingStealthCheckEvent : IInventoryRelayEvent
{
    public bool Stealthy;

    public SlotFlags TargetSlots => SlotFlags.GLOVES;
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;


namespace Content.Trauma.Common.Projectiles;

/// <summary>
/// Raised on the entity that got hit by a projectile.
/// </summary>
[ByRefEvent]
public record struct GotHitByProjectileEvent(EntityUid Projectile) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;
}

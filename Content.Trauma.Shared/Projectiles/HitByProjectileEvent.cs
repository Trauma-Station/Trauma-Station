// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;
using Content.Shared.Damage;
using Content.Shared.Inventory;


namespace Content.Trauma.Shared.Projectiles;

/// <summary>
///     Event raised on entities that have been hit.
/// </summary>
public sealed class HitByProjectileEvent : EntityEventArgs, IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;

    /// <summary>
    ///     Entity that got hit.
    /// </summary>
    public EntityUid Target { get; }

    public HitByProjectileEvent(EntityUid target)
    {
        Target = target;
    }
}

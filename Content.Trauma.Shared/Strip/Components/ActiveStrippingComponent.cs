// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Strip.Components;

/// <summary>
/// Tracks the number of active strip doafters this entity is currently performing.
/// Each active doafter "uses" one virtual hand slot.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActiveStrippingComponent : Component
{
    [DataField, AutoNetworkedField]
    public int ActiveCount;

    // Not networked, server-side only, tracks active doafter indices to avoid double-counting.
    public HashSet<ushort> TrackedDoAfters = new();

    // Not networked, tracks storages opened via bag access so IgnoreUIRangeComponent is cleaned up on close.
    [DataField]
    public HashSet<EntityUid> BagAccessOpenedStorages = new();
}

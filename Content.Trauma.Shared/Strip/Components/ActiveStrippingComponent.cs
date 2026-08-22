// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Strip.Components;

/// <summary>
/// Tracks the number of active strip doafters this entity is currently performing.
/// Each active doafter "uses" one virtual hand slot.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ActiveStrippingComponent : Component
{
    [DataField, AutoNetworkedField]
    public int ActiveCount;

    // Not networked, server-side only, tracks active doafter indices to avoid double-counting.
    public HashSet<ushort> TrackedDoAfters = new();

    // Not networked, tracks storages opened via bag access so IgnoreUIRangeComponent is cleaned up on close.
    [DataField]
    public HashSet<EntityUid> BagAccessOpenedStorages = new();

    /// <summary>
    /// Server time at which bag-access range is next checked for this entity.
    /// Only relevant while BagAccessOpenedStorages is non-empty.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextBagAccessCheck = TimeSpan.Zero;

    /// <summary>
    /// How often bag-access range is checked while a storage is open this way.
    /// </summary>
    [DataField]
    public TimeSpan BagAccessCheckInterval = TimeSpan.FromSeconds(0.5);
}

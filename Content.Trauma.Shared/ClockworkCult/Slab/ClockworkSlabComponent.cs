// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.ClockworkCult.Slab;

/// <summary>
/// Holds scriptures and related data
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(ClockworkSlabSystem))]
public sealed partial class ClockworkSlabComponent : Component
{
    /// <summary>
    /// The current entity that is holding the slab.
    /// Applies if its in inventory slots.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Holder;

    /// <summary>
    /// How often to try to transfer power to the holder.
    /// </summary>
    [DataField]
    public TimeSpan Update = TimeSpan.FromSeconds(1f); // TODO: Higher?

    /// <summary>
    /// How much power to try to transfer every update
    /// </summary>
    [DataField]
    public float Charge = 10;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextUpdate;
}

[Serializable, NetSerializable]
public enum ClockworkSlabUiKey : byte
{
    Key
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Server.FireControl;

[RegisterComponent]
public sealed partial class FireControllableComponent : Component
{
    /// <summary>
    /// Reference to the controlling server, if any.
    /// </summary>
    [ViewVariables]
    public EntityUid? ControllingServer = null;

    /// <summary>
    /// When the weapon can next be fired
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextFire = TimeSpan.Zero;

    /// <summary>
    /// Cooldown between firing, in seconds
    /// </summary>
    [DataField]
    public float FireCooldown = 0.2f;
}

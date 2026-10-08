// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Drone;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class DroneComponent : Component
{
    [DataField]
    public float InteractionBlockRange = 1.5f;

    /// delay before posting another proximity alert
    [DataField]
    public TimeSpan ProximityDelay = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextProximityAlert;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;
}

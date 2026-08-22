// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Nuclear.Monitor;

/// <summary>
/// Component for nuclear reactor/turbine monitors that handles linking.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class NuclearMonitorComponent : Component
{
    /// <summary>
    /// The port to use for linking the machine.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SinkPortPrototype> LinkingPort;

    /// <summary>
    /// Whitelist for the machine that can be linked to this monitor.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = default!;

    /// <summary>
    /// The machine this monitor is currently linked to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Linked;
}

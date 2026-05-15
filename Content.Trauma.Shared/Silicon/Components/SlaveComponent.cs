// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Silicon.Components;

/// <summary>
/// Designate's a robot's master.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlaveComponent : Component
{
    /// <summary>
    /// The master.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? MasterEntity { get; set; }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Silicon.Components;

/// <summary>
/// Designate's a securitron robot's master for patrolling.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PatrolSlaveComponent : Component
{
    /// <summary>
    /// The master that bot points to for patrol instructions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? MasterEntity;
}

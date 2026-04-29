// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Silicon.Components;

/// <summary>
/// Designate's a robot's master.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlaveComponent : Component
{
    /// <summary>
    /// The master.
    /// </summary>
    [DataField]
    public EntityUid? MasterEntity { get; set; }

    /// <summary>
    /// Should the slave be patrolling?
    /// </summary>
    [DataField]
    public bool IsPatrolling;
}

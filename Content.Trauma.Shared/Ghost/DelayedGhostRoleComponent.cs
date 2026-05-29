// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;

namespace Content.Trauma.Shared.Ghost;

[RegisterComponent, NetworkedComponent]
public sealed partial class DelayedGhostRoleComponent : Component
{
    /// <summary>
    /// Whether to use FastReinforcementDelay instead of SlowReinforcementDelay.
    /// </summary>
    [DataField]
    public bool Fast;

    /// <summary>
    /// The name shown on the Ghost Role list
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// The description shown on the Ghost Role list
    /// </summary>
    [DataField(required: true)]
    public LocId Description;

    /// <summary>
    /// The introductory message shown when trying to take the ghost role/join the raffle
    /// </summary>
    [DataField(required: true)]
    public LocId Rules;

    /// <summary>
    /// A list of mind roles that will be added to the entity's mind
    /// </summary>
    [DataField]
    public List<EntProtoId> MindRoles = new() { "MindRoleGhostRoleNeutral" };

    /// <summary>
    /// The prototype ID of the job that will be given to the controlling mind
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? Job;
}

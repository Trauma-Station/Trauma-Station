// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// A component attached to an objective entity to make it into a side job.
/// Every side job has this component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class SideJobComponent : Component
{
    /// <summary>
    /// The entity spawned as a reward for completing this side job.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? Reward;

    /// <summary>
    /// The real name of the reward entity.
    /// The entity name is often disguised (like stealth box being called 'cardboard box') so we take the real name from the uplink entry.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? RewardName;

    /// <summary>
    /// The entity spawned when the side job is accepted, to be used to complete the job.
    /// For example, it could be a bug that must be planted in an office.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? Tool;

    /// <summary>
    /// How much reputation you gain from completing the mission.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ReputationGain;

    /// <summary>
    /// The minimum reputation level you must have for this job to be offered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MinimumLevel;

    /// <summary>
    /// If this side job can be repeated. Theft objectives can't be repeated while murder objectives can be.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Repeatable = false;

    /// <summary>
    /// Objective progress is handled server-side.
    /// Somethings are infact impossible to predict, like if someone is alive (because they could be out of PVS).
    /// The side job can be updated and this will cache and network the progress.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CachedProgress;
}

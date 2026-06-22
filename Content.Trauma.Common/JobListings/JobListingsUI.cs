// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.JobListings;

/// <summary>
/// Message sent by pda bui to open job listings bui.
/// </summary>
[Serializable, NetSerializable]
public sealed class PdaShowJobListingsMessage : BoundUserInterfaceMessage;

/// <summary>
/// Message send by job listings bui to accept an available job.
/// </summary>
[Serializable, NetSerializable]
public sealed class JobListingsAcceptJobMessage(NetEntity job) : BoundUserInterfaceMessage
{
    public NetEntity Job = job;
}

/// <summary>
/// Message send by job listings bui to accept an available job.
/// </summary>
[Serializable, NetSerializable]
public sealed class JobListingsCancelJobMessage(NetEntity job) : BoundUserInterfaceMessage
{
    public NetEntity Job = job;
}

/// <summary>
/// Message send by job listings bui to claim the reward for a completed accepted job.
/// </summary>
[Serializable, NetSerializable]
public sealed class JobListingsClaimJobMessage(NetEntity job) : BoundUserInterfaceMessage
{
    public NetEntity Job = job;
}

/// <summary>
/// Message sent by job listings bui to force a new updated state to be sent by the client.
/// </summary>
[Serializable, NetSerializable]
public sealed class JobListingsForceReloadMessage : BoundUserInterfaceMessage;

/// <summary>
/// Info describing a side job.
/// </summary>
[Serializable, NetSerializable]
public record struct SideJobInfo(string Title, string Description, SpriteSpecifier Icon, float Progress, string RewardName, int ReputationGain, NetEntity Entity);

/// <summary>
/// Bui state describing a job board.
/// </summary>
[Serializable, NetSerializable]
public sealed class JobListingsUserInterfaceState(List<SideJobInfo> availableSideJobs, List<SideJobInfo> acceptedSideJobs, int reputation, int reputationLevel, int maximumAcceptedSideJob) : BoundUserInterfaceState
{
    public readonly List<SideJobInfo> AvailableSideJobs = availableSideJobs;
    public readonly List<SideJobInfo> AcceptedSideJobs = acceptedSideJobs;
    public readonly int Reputation = reputation;
    public readonly int ReputationLevel = reputationLevel;
    public readonly int MaximumAcceptedSideJobs = maximumAcceptedSideJob;
}

[Serializable, NetSerializable]
public enum JobListingsUiKey
{
    Key
}

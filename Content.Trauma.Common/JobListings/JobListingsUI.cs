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
/// Message send by job listings bui when the refresh button is pressed.
/// </summary>
[Serializable, NetSerializable]
public sealed class JobListingsRefreshMessage : BoundUserInterfaceMessage;

/// <summary>
/// Struct that describes a SideJob entity.
/// </summary>
[Serializable, NetSerializable]
public record struct SideJobInfo(NetEntity Entity, float Progress, string Title, string Description, SpriteSpecifier Icon, string RewardName, int ReputationGain);

/// <summary>
/// The BoundUserInterfaceState used to update the job board.
/// </summary>
[Serializable, NetSerializable]
public sealed class JobListingsBUI(List<SideJobInfo> availableSideJobs, List<SideJobInfo> acceptedSideJobs, int reputation, int reputationLevel, bool bonusRefresh, TimeSpan? refreshTime, TimeSpan refreshWaitDuration, int maximumAcceptedSideJobs, bool loading) : BoundUserInterfaceState
{
    public readonly List<SideJobInfo> AvailableSidejobs = availableSideJobs;
    public readonly List<SideJobInfo> AcceptedSideJobs = acceptedSideJobs;
    public readonly int Reputation = reputation;
    public readonly int ReputationLevel = reputationLevel;
    public bool BonusRefresh = bonusRefresh;
    public TimeSpan? RefreshTime = refreshTime;
    public TimeSpan RefreshWaitDuration = refreshWaitDuration;
    public int MaximumAcceptedSideJobs = maximumAcceptedSideJobs;
    public bool Loading = loading;

}

[Serializable, NetSerializable]
public enum JobListingsUiKey : byte
{
    Key
}

// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.JobListings;

[Serializable, NetSerializable]
public sealed class PdaShowJobListingsMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class JobListingsAcceptJobMessage(NetEntity job) : BoundUserInterfaceMessage
{
    public NetEntity Job = job;
}

[Serializable, NetSerializable]
public sealed class JobListingsCancelJobMessage(NetEntity job) : BoundUserInterfaceMessage
{
    public NetEntity Job = job;
}

[Serializable, NetSerializable]
public record struct SideJobInfo(string Title, string Description, SpriteSpecifier Icon, float Progress, string RewardName, int ReputationGain, NetEntity Entity);

[Serializable, NetSerializable]
public sealed class JobListingsUserInterfaceState(List<SideJobInfo> availableSideJobs, List<SideJobInfo> acceptedSideJobs, int maximumAcceptedSideJob) : BoundUserInterfaceState
{
    public readonly List<SideJobInfo> AvailableSideJobs = availableSideJobs;
    public readonly List<SideJobInfo> AcceptedSideJobs = acceptedSideJobs;
    public readonly int MaximumAcceptedSideJobs = maximumAcceptedSideJob;
}

[Serializable, NetSerializable]
public enum JobListingsUiKey
{
    Key
}

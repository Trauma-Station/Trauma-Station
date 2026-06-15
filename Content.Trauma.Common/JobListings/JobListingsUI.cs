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
public record struct SideJobInfo(string Title, string Description, SpriteSpecifier Icon, float Progress, string RewardName, NetEntity Entity);

[Serializable, NetSerializable]
public sealed class JobListingsUserInterfaceState(List<SideJobInfo> availableSideJobs, List<SideJobInfo> acceptedSideJobs) : BoundUserInterfaceState
{
    public List<SideJobInfo> AvailableSideJobs = availableSideJobs;
    public List<SideJobInfo> AcceptedSideJobs = acceptedSideJobs;
}

[Serializable, NetSerializable]
public enum JobListingsUiKey
{
    Key
}

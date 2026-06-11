// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.JobListings;

[Serializable, NetSerializable]
public sealed class PdaShowJobListingsMessage : BoundUserInterfaceMessage
{
    public PdaShowJobListingsMessage() { }
}

[Serializable, NetSerializable]
public record struct SideJobInfo(string Title, string Description, SpriteSpecifier Icon, float Progress);

[Serializable, NetSerializable]
public sealed class JobListingsUserInterfaceState(List<SideJobInfo> availableSideJobs) : BoundUserInterfaceState
{
    public List<SideJobInfo> AvailableSideJobs = availableSideJobs;
}

[Serializable, NetSerializable]
public enum JobListingsUiKey
{
    Key
}

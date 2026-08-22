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
public sealed class JobListingsRefreshMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public enum JobListingsUiKey
{
    Key
}

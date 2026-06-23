// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityTable;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// Component added to a store entity to enable side-jobs.
/// Used for progressive traitor.
/// </summary>

[RegisterComponent]
public sealed partial class JobListingsComponent : Component
{
    /// <summary>
    /// How many jobs can exist at once.
    /// </summary>
    [DataField]
    public int MaximumSideJobs;

    /// <summary>
    /// List of prototypes of the objectives offered for medium side jobs.
    /// </summary>
    [DataField]
    public List<EntProtoId> MediumSideJobOffers = new();

    /// <summary>
    /// The list of available side jobs which can be accepted by the traitor.
    /// </summary>
    [DataField]
    public List<EntityUid> AvailableSideJobs = new();

    /// <summary>
    /// How many jobs can be accepted at once.
    /// </summary>
    [DataField]
    public int MaximumAcceptedSideJobs;

    /// <summary>
    /// The list of side jobs the traitor has accepted.
    /// </summary>
    [DataField]
    public List<EntityUid> AcceptedSideJobs = new();

    /// <summary>
    ///  The mind of the person (probably traitor) who owns the job board.
    /// </summary>
    [DataField]
    public EntityUid? Mind;

    /// <summary>
    /// The reputation of the traitor who owns the job board.
    /// </summary>
    [DataField]
    public int Reputation = 0;

    /// <summary>
    /// Reputation required to reach each level.
    /// Level 0 by default, first element in list is rep required for level 1, second element is rep required for level 2, and so on.
    /// </summary>
    [DataField]
    public List<int> ReputationLevels;

    /// <summary>
    /// A list of <see cref="RemoteJobListingsComponent"/> that are targeting this entity.
    /// Should only be edited by <see cref="JobListingsSystem.Link"/>
    /// </summary>
    [DataField]
    public List<EntityUid> Remotes;
}

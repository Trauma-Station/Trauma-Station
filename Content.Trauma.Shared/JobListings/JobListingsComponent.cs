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
    public int SideJobCount;

    /// <summary>
    /// List of prototypes of the objectives offered for medium side jobs.
    /// </summary>
    [DataField]
    public List<EntProtoId> MediumSideJobOffers = new();

    /// <summary>
    /// The list of available side jobs which can be accepted by the traitor.
    /// </summary>
    [DataField]
    public List<SideJob> AvailableSideJobs = new();

    /// <summary>
    /// How many jobs can be accepted at once.
    /// </summary>
    [DataField]
    public int AcceptedSideJobCount;

    /// <summary>
    /// The list of side jobs the traitor has accepted.
    /// </summary>
    [DataField]
    public List<SideJob> AcceptedSideJobs = new();

    /// <summary>
    ///  The mind of the person (probably traitor) who owns the job board.
    /// </summary>
    [DataField]
    public EntityUid? Mind;
}

/// <summary>
/// A struct that stores all necessary information about a side job.
/// </summary>
/// <param name="Entity"></param>
/// <param name="Prototype"></param>
public record struct SideJob(EntityUid Entity, EntProtoId Prototype);

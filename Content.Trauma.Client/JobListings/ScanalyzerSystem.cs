// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Objectives;
using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Client.JobListings;

/// <inheritdoc/>
public sealed partial class ScanalyzerSystem : SharedScanalyzerSystem
{
    [Dependency] private JobListingsSystem _jobListings = default!;

    protected override void AfterScan(Entity<ScanalyzerComponent> entity, EntityUid user, ProtoId<StealTargetGroupPrototype> target)
    {
        _jobListings.ForceJobListingsBuiReload();
    }
}

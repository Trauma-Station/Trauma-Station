// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Containers;

namespace Content.Trauma.Common.JobListings;

public abstract partial class SharedJobListingsSystem : EntitySystem
{
    [Dependency] protected SharedContainerSystem _container = default!;

    public bool TryGetSideJobs(Entity<JobListingsComponent?> ent, [NotNullWhen(true)] out List<NetEntity>? result)
    {
        result = null;

        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        ent.Comp.AvailableSideJobsContainer = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.AvailableSideJobsContainerId);
        result = ent.Comp.AvailableSideJobsContainer.ContainedEntities.Select(x => GetNetEntity(x)).ToList();
        return true;
    }
}

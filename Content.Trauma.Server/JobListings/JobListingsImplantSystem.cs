// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Server.JobListings;

public sealed partial class JobListingsImplantSystem : SharedJobListingsImplantSystem
{
    [Dependency] private JobListingsSystem _job = default!;

    [SubscribeLocalEvent]
    private void OnImplantUsed(Entity<JobListingsImplantComponent> ent, ref OpenJobListingsImplantEvent args)
    {
        if (args.Handled)
            return;
        _job.OpenUi(ent.Owner, args.Performer);
        args.Handled = true;
    }
}

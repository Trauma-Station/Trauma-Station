// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Server.JobListings;

public sealed partial class JobListingsImplantSystem : SharedJobListingsImplantSystem
{
    [Dependency] private JobListingsSystem _job = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JobListingsImplantComponent, OpenJobListingsImplantEvent>(OnImplantUsed);
    }

    private void OnImplantUsed(Entity<JobListingsImplantComponent> entity, ref OpenJobListingsImplantEvent args)
    {
        if (args.Handled)
            return;
        _job.OpenUi(entity.Owner, args.Performer);
        args.Handled = true;
    }
}

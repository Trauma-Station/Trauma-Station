// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.JobListings;
using Robust.Shared.Player;

namespace Content.Trauma.Client.JobListings;

public sealed class JobListingsSystem : SharedJobListingsSystem
{
    public override void OpenUi(Entity<JobListingsComponent> ent)
    {
        _ui.OpenUi(ent.Owner, JobListingsUiKey.Key);
    }
}

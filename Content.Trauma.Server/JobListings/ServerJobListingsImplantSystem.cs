// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Traitor.Uplink;
using Content.Shared.Implants;
using Content.Trauma.Common.JobListings;
using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Server.JobListings;

/// <inheritdoc />
public sealed partial class ServerJobListingsImplantSystem : JobListingsImplantSystem
{
    [SubscribeLocalEvent(after: [typeof(UplinkSystem)])]
    private void OnImplantImplanted(Entity<JobListingsImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        if (HasComp<RemoteJobListingsComponent>(ent))
            AddAction(ent);
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.JobListings;
using Content.Trauma.Shared.JobListings;
using Robust.Shared.Player;

namespace Content.Trauma.Client.JobListings;

public sealed partial class JobListingsSystem : SharedJobListingsSystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;

    /// <summary>
    /// Find all open JobListingsBuis on this client and force reload them by sending a message to the server.
    /// </summary>
    public void ForceJobListingsBuiReload()
    {
        var query = EntityQueryEnumerator<RemoteJobListingsComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out _, out var ui))
        {
            if (!_ui.TryGetOpenUi((uid, ui), JobListingsUiKey.Key, out var bui))
                continue;
            if (bui is not JobListingsBoundUserInterface jobListingsBui)
                continue;
            jobListingsBui.ForceReload();
        }
    }
}

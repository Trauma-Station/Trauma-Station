// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.PDA;
using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Client.JobListings;

public sealed partial class ClientJobListingsSystem : JobListingsSystem
{
    public override void Initialize()
    {
        base.Initialize();
        PdaBoundUserInterface.OnMenuChanged += OnMenuCreated;
    }

    private void OnMenuCreated(EntityUid owner, PdaMenu menu)
    {
        if (!TryComp<RemoteJobListingsComponent>(owner, out var remoteComp))
            return;
        Log.Warning("menu created");
        if (IsJobBoardHidden((owner, remoteComp)))
            menu.ShowJobListingsButton.Visible = false;
    }
}

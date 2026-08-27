// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Trauma.Common.JobListings;
using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Client.JobListings;

public sealed partial class JobListingsSystem : SharedJobListingsSystem
{
    public override void UpdateUi(EntityUid owner, EntityUid actor)
    {
        UpdateUi(owner);
    }

    private void UpdateUi(EntityUid owner)
    {
        if (Ui.TryGetOpenUi(owner, JobListingsUiKey.Key, out var bui))
            bui.Update();
    }

    [SubscribeNetworkEvent]
    private void OnUpdateUi(JobListingsUiUpdateMessage msg, EntitySessionEventArgs args)
    {
        UpdateUi(GetEntity(msg.Owner));
    }
}

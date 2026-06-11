// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Content.Trauma.Common.JobListings;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.JobListings;

public abstract partial class SharedJobListingsSystem : EntitySystem
{
    [Dependency] protected SharedObjectivesSystem _objectives = default!;
    [Dependency] protected IPrototypeManager _proto = default!;

    public SideJobInfo? GetInfo(EntityUid mind, EntityUid sideJob)
    {
        var basic = _objectives.GetInfo(sideJob, mind);
        if (basic is null)
            return null;
        if (!TryComp<SideJobComponent>(sideJob, out var sideJobComp))
            return null;
        if (sideJobComp.Reward is null)
            return null;
        if (!_proto.Resolve(sideJobComp.Reward.Value, out var rewardProto))
            return null;

        var name = Loc.GetString($"job-listings-ui-reward-name-{rewardProto.ID}");
        return new SideJobInfo(basic.Value.Title, basic.Value.Description, basic.Value.Icon, basic.Value.Progress, name);
    }
}

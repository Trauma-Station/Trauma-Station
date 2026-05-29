// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Objectives.Components;
using Content.Trauma.Server.Salvage;

namespace Content.Trauma.Server.Objectives;

public sealed partial class ClaimPointsConditionSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private MiningRewardsSystem _rewards = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClaimPointsConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<ClaimPointsConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAssigned(Entity<ClaimPointsConditionComponent> ent, ref ObjectiveAssignedEvent args)
    {
        var desc = Loc.GetString(ent.Comp.Desc, ("quota", ent.Comp.Quota));
        _meta.SetEntityDescription(ent.Owner, desc);
    }

    private void OnGetProgress(Entity<ClaimPointsConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = args.Mind.UserId is {} id
            ? (float) _rewards.GetPointsClaimed(id) / ent.Comp.Quota
            : 0f;
    }
}

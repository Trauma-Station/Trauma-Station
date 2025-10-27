using Content.Shared.Objectives.Components;
using Content.Trauma.Server.ServerCurrency;

namespace Content.Trauma.Server.Objectives;

public sealed class ClaimPointsConditionSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MiningRewardsSystem _rewards = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClaimPointsConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<ClaimPointsConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAssigned(Entity<ClaimPointsConditionComponent> ent, ref ObjectiveAssignedEvent args)
    {
        var desc = Loc.GetString(ent.Comp.Desc, ("quota", _rewards.PointsQuota));
        _meta.SetEntityDescription(ent.Owner, desc);
    }

    private void OnGetProgress(Entity<ClaimPointsConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = args.Mind.UserId is {} id
            ? (float) _rewards.GetPointsClaimed(id) / _rewards.PointsQuota
            : 0f;
    }
}

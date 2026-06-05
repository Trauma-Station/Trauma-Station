// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Mind;
using Content.Server.Objectives.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.Objectives.Components;
using Content.Trauma.Shared.Vampires;

namespace Content.Trauma.Server.Vampires.Objectives;

/// <summary>
/// This handles anything related to vampire objectives
/// </summary>
public sealed partial class VampireObjectivesSystem : EntitySystem
{
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private NumberObjectiveSystem _number = default!;
    [Dependency] private EntityQuery<VampireBloodConditionComponent> _bloodQuery = default!;
    [Dependency] private EntityQuery<VampireSuckConditionComponent> _suckQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindContainerComponent, VampireTotalBloodChangedEvent>(OnTotalBloodChanged);
        SubscribeLocalEvent<VampireBloodConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        SubscribeLocalEvent<MindContainerComponent, BloodsuckingSuccessEvent>(OnBloodsucking);
        SubscribeLocalEvent<VampireSuckConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    #region Blood Condition
    private void OnTotalBloodChanged(Entity<MindContainerComponent> ent, ref VampireTotalBloodChangedEvent args)
    {
        if (ent.Comp.Mind is not { } mind)
            return;

        foreach (var objId in _mind.EnumerateObjectives(mind))
        {
            if (_bloodQuery.TryGetComponent(objId, out var obj))
            {
                obj.Blood = args.Blood;
                break;
            }
        }
    }

    private void OnGetProgress(Entity<VampireBloodConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(ent.Comp.Blood, _number.GetTarget(ent));
    }
    #endregion


    #region Bloodsuck Condition
    private void OnBloodsucking(Entity<MindContainerComponent> ent, ref BloodsuckingSuccessEvent args)
    {
        if (ent.Comp.Mind is not { } mind)
            return;

        foreach (var objId in _mind.EnumerateObjectives(mind))
        {
            if (_suckQuery.TryGetComponent(objId, out var obj))
            {
                obj.SuckedEntities.Add(args.TargetSucked);
                break;
            }
        }
    }

    private void OnGetProgress(Entity<VampireSuckConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(ent.Comp.SuckedEntities.Count, _number.GetTarget(ent));
    }
    #endregion

    #region Helper
    private float GetProgress(int number, int target)
    {
        if (target == 0)
            return 1f;

        if (number >= target)
            return 1f;

        return (float) number / target;
    }
    #endregion
}

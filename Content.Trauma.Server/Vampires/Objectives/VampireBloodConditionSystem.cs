// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Mind;
using Content.Server.Objectives.Systems;
using Content.Shared.Objectives.Components;
using Content.Trauma.Shared.Vampires;

namespace Content.Trauma.Server.Vampires.Objectives;

public sealed partial class VampireBloodConditionSystem : EntitySystem
{
    [Dependency] private NumberObjectiveSystem _number = default!; // please move this shit to shared
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private EntityQuery<VampireBloodConditionComponent> _objQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, VampireTotalBloodChangedEvent>(OnBloodChanged);
        SubscribeLocalEvent<VampireBloodConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<VampireBloodConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(ent.Comp, _number.GetTarget(ent));
    }

    private void OnBloodChanged(Entity<VampireComponent> ent, ref VampireTotalBloodChangedEvent args)
    {
        if (!_mind.TryGetMind(ent, out _, out var mind))
            return;

        foreach (var objId in mind.Objectives)
        {
            if (_objQuery.TryGetComponent(objId, out var obj))
            {
                AddBlood((objId, obj), args.Blood);
                break;
            }
        }
    }

    private float GetProgress(VampireBloodConditionComponent comp, int target)
    {
        if (target == 0)
            return 1f;

        if (comp.Blood >= target)
            return 1f;

        return (float) comp.Blood / target;
    }

    /// <summary>
    /// Sets the blood of the objective, called after a bloodsucking sequence
    /// </summary>
    public void AddBlood(Entity<VampireBloodConditionComponent?> ent, int blood)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.Blood = blood;
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Antag;
using Content.Server.Objectives.Systems;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Content.Trauma.Shared.Abductor;
using Content.Trauma.Shared.Roles;

namespace Content.Trauma.Server.Abductor;

/// <summary>
/// Manages experiment task count going up and tying abductors to their rule.
/// </summary>
public sealed class AbductorRuleSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbductorRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelected);
        SubscribeLocalEvent<MindContainerComponent, AbductorTaskCompleteEvent>(OnTaskComplete);

        SubscribeLocalEvent<AbductorTasksConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    // TODO: this should be a generic gamerule tracking mind role component
    private void OnAntagSelected(Entity<AbductorRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (_mind.GetMind(args.EntityUid) is not {} mind ||
            !_role.MindHasRole<AbductorRoleComponent>(mind, out var role))
            return;

        role.Value.Comp2.Rule = ent;
    }

    private void OnTaskComplete(Entity<MindContainerComponent> ent, ref AbductorTaskCompleteEvent args)
    {
        if (ent.Comp.Mind is not {} mind ||
            !_role.MindHasRole<AbductorRoleComponent>(mind, out var role) ||
            role.Value.Comp2.Rule is not {} rule ||
            !TryComp<AbductorRuleComponent>(rule, out var comp))
            return;

        comp.TasksCompleted++;
    }

    private void OnGetProgress(Entity<AbductorTasksConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = AbductProgress((args.MindId, args.Mind), _number.GetTarget(ent.Owner));
    }

    private float AbductProgress(Entity<MindComponent?> mind, int target)
    {
        if (!_role.MindHasRole<AbductorRoleComponent>(mind, out var role) ||
            role.Value.Comp2.Rule is not {} rule ||
            !TryComp<AbductorRuleComponent>(rule, out var comp))
            return 0f; // can't track it without a rule :(

        return target == 0 ? 1f : MathF.Min(comp.TasksCompleted / (float) target, 1f);
    }
}

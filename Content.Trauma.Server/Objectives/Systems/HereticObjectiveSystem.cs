// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Mind;
using Content.Server.Objectives.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Trauma.Server.Heretic.Systems;
using Content.Trauma.Server.Objectives.Components;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Server.Heretic.Objectives;

public sealed partial class HereticObjectiveSystem : EntitySystem
{
    [Dependency] private NumberObjectiveSystem _number = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private HereticSystem _heretic = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticKnowledgeConditionComponent, ObjectiveGetProgressEvent>(OnGetKnowledgeProgress);
        SubscribeLocalEvent<HereticSacrificeConditionComponent, ObjectiveGetProgressEvent>(OnGetSacrificeProgress);

        SubscribeLocalEvent<HereticComponent, IncrementHereticObjectiveProgressEvent>(OnIncrement);
    }

    private void OnIncrement(Entity<HereticComponent> ent, ref IncrementHereticObjectiveProgressEvent args)
    {
        if (!TryComp(ent, out MindComponent? mind))
            return;

        if (!_mind.TryFindObjective((ent.Owner, mind), args.Proto, out var obj))
            return;

        CompOrNull<HereticSacrificeConditionComponent>(obj.Value)?.Sacrificed += args.Amount;

        _heretic.UpdateObjectiveProgress((ent, ent.Comp, mind));
    }

    private void OnGetKnowledgeProgress(Entity<HereticKnowledgeConditionComponent> ent,
        ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(ent);
        args.Progress = target != 0 ? MathF.Min(ent.Comp.Researched / target, 1f) : 1f;
    }

    private void OnGetSacrificeProgress(Entity<HereticSacrificeConditionComponent> ent,
        ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(ent);
        args.Progress = target != 0 ? MathF.Min(ent.Comp.Sacrificed / target, 1f) : 1f;
    }
}

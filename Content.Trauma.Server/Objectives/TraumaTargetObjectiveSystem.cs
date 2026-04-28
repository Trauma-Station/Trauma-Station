// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Mind;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.Roles.Jobs;
using Content.Trauma.Server.Wizard.Components;

namespace Content.Trauma.Server.Objectives;

public sealed class TraumaTargetObjectiveSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DynamicObjectiveTargetMindComponent, MindGotAddedEvent>(OnMindAdded);
        SubscribeLocalEvent<EntityRenamedEvent>(OnRenamed);
    }

    private void OnMindAdded(Entity<DynamicObjectiveTargetMindComponent> ent, ref MindGotAddedEvent args)
    {
        UpdateAllDynamicObjectiveNamesWithTarget(ent.Owner);
    }

    private void OnRenamed(ref EntityRenamedEvent ev)
    {
        if (_mind.TryGetMind(ev.Uid, out var mind, out _) && HasComp<DynamicObjectiveTargetMindComponent>(mind))
            UpdateAllDynamicObjectiveNamesWithTarget(mind);
    }

    private void UpdateAllDynamicObjectiveNamesWithTarget(EntityUid target)
    {
        var query = AllEntityQuery<TargetObjectiveComponent, MetaDataComponent>();

        while (query.MoveNext(out var uid, out var comp, out var meta))
        {
            if (!comp.DynamicName || comp.Target != target)
                continue;

            _metaData.SetEntityName(uid, _target.GetTitle(target, comp.Title, true, comp.ShowJobTitle), meta);
        }
    }

    public void SetName(EntityUid uid, TargetObjectiveComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (!_target.GetTarget(uid, out var target, comp))
            return;

        _metaData.SetEntityName(uid, _target.GetTitle(target.Value, comp.Title, comp.DynamicName, comp.ShowJobTitle));
    }
}

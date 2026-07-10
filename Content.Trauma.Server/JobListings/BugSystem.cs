// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.JobListings;
using Content.Shared.Construction.Components;
using Content.Server.Mind;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;
using Content.Shared.Objectives.Systems;
using Content.Shared.Objectives.Components;

namespace Content.Trauma.Server.JobListings;

/// <inheritdoc/>
public sealed partial class BugSystem : SharedBugSystem
{
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private JobListingsSystem _jobs = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BugAreaConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<BugAreaConditionComponent, ObjectiveGetProgressEvent>(OnGetObjectiveProgress);
        SubscribeLocalEvent<BugComponent, UserAnchoredEvent>(OnWrench);
    }

    /// <summary>
    /// Register an area as bugged.
    /// </summary>
    public void RegisterBuggedArea(Entity<MindComponent> mind, EntProtoId area)
    {
        var archive = EnsureComp<BugMindArchiveComponent>(mind.Owner);
        if (!archive.BuggedAreas.Contains(area))
            archive.BuggedAreas.Add(area);
    }

    /// <summary>
    /// Finds if an area has been bugged.
    /// </summary>
    public bool IsAreaBugged(Entity<MindComponent> mind, EntProtoId area)
    {
        if (!TryComp<BugMindArchiveComponent>(mind.Owner, out var archive))
            return false;
        return archive.BuggedAreas.Contains(area);
    }

    private void OnAssigned(Entity<BugAreaConditionComponent> entity, ref ObjectiveAssignedEvent args)
    {
        if (!GetAreaName(entity.Comp.TargetArea, out var name))
            return;

        _metaData.SetEntityName(entity.Owner, Loc.GetString("bug-objective-name", ("area", name)));
        _metaData.SetEntityDescription(entity.Owner, Loc.GetString("bug-objective-description", ("area", name)));
        _objectives.SetIcon(entity.Owner, new SpriteSpecifier.EntityPrototype(entity.Comp.IconEntity));
    }

    private void OnGetObjectiveProgress(Entity<BugAreaConditionComponent> entity, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 0f;
        if (IsAreaBugged((args.MindId, args.Mind), entity.Comp.TargetArea))
            args.Progress = 1f;
    }

    private void OnWrench(Entity<BugComponent> entity, ref UserAnchoredEvent args)
    {
        if (!Transform(entity.Owner).Anchored || !IsInCorrectArea(entity))
            return;
        if (!_mind.TryGetMind(args.User, out var mind, out var mindComp))
            return;

        RegisterBuggedArea((mind, mindComp), entity.Comp.TargetArea);
        _jobs.UpdateUis((mind, mindComp));
    }
}

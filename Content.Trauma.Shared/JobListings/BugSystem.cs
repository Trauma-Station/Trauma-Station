// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Construction.Components;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Trauma.Shared.Areas;

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// System facilitating planting bugs in head of staff offices for traitor objectives.
/// The list of areas which bugs have been planted into is stored in the traitor's mind inside <see cref="BugMindArchiveComponent"/>.
/// </summary>
public sealed partial class BugSystem : EntitySystem
{
    [Dependency] private AreaSystem _area = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private JobListingsSystem _jobs = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedObjectivesSystem _objectives = default!;
    [Dependency] private EntityQuery<MindComponent> _mindQuery = default!;

    /// <summmary>
    /// Works out if the bug is in the correct area.
    /// </summary>
    public bool IsInCorrectArea(Entity<BugComponent> ent)
    {
        if (_area.GetArea(ent) is not { } area)
            return false;

        return Prototype(area)?.ID == ent.Comp.TargetArea;
    }

    /// <summary>
    /// Register an area as bugged.
    /// </summary>
    public void RegisterBuggedArea(Entity<MindComponent> mind, EntProtoId area)
    {
        var archive = EnsureComp<BugMindArchiveComponent>(mind.Owner);
        if (archive.BuggedAreas.Add(area))
            Dirty(mind.Owner, archive);
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

    [SubscribeLocalEvent]
    private void OnExamine(Entity<BugComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.TargetArea is not { } area)
        {
            Log.Error($"Bug {ToPrettyString(ent)}'s TargetArea is not set.");
            return;
        }

        var name = ProtoMan.Index(area).Name;
        args.PushMarkup(Loc.GetString("bug-examine-target-area", ("target-area", name)));

        if (Transform(ent).Anchored)
        {
            var prefix = IsInCorrectArea(ent) ? "" : "in";
            args.PushMarkup(Loc.GetString($"bug-examine-{prefix}correct-area"));
        }
    }

    [SubscribeLocalEvent]
    private void OnAssigned(Entity<BugAreaConditionComponent> ent, ref ObjectiveAssignedEvent args)
    {
        if (ent.Comp.TargetArea is not { } area)
        {
            Log.Error($"Bug {ToPrettyString(ent)}'s TargetArea is not set.");
            return;
        }

        var name = ProtoMan.Index(area).Name;
        _metaData.SetEntityName(ent.Owner, Loc.GetString(ent.Comp.ObjectiveName, ("area", name)));
        _metaData.SetEntityDescription(ent.Owner, Loc.GetString(ent.Comp.ObjectiveDescription, ("area", name)));
        _objectives.SetIcon(ent.Owner, new SpriteSpecifier.EntityPrototype(ent.Comp.IconEntity));
    }

    [SubscribeLocalEvent]
    private void OnGetObjectiveProgress(Entity<BugAreaConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (ent.Comp.TargetArea is not { } area)
        {
            Log.Error($"BugObjective {ToPrettyString(ent)}'s TargetArea is not set.");
            return;
        }

        args.Progress = 0f;
        if (IsAreaBugged((args.MindId, args.Mind), area))
            args.Progress = 1f;
    }

    [SubscribeLocalEvent]
    private void OnWrench(Entity<BugComponent> ent, ref UserAnchoredEvent args)
    {
        if (!Transform(ent.Owner).Anchored || !IsInCorrectArea(ent) ||
            !_mindQuery.TryComp(ent.Comp.Mind, out var mindComp))
            return;

        if (ent.Comp.TargetArea is not { } area)
        {
            Log.Error($"Bug {ToPrettyString(ent)}'s TargetArea is not set.");
            return;
        }

        var mind = (ent.Comp.Mind.Value, mindComp);
        RegisterBuggedArea(mind, area);
        _jobs.UpdateUi(mind);
    }

    [SubscribeLocalEvent]
    private void OnToolSpawned(Entity<BugComponent> ent, ref SideJobToolSpawnedEvent args)
    {
        if (!TryComp<BugAreaConditionComponent>(args.Objective, out var objComp))
            return;

        ent.Comp.TargetArea ??= objComp.TargetArea;
        ent.Comp.Mind ??= args.Mind;
        Dirty(ent);
    }
}

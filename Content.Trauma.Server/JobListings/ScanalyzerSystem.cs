// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Mind;
using Content.Server.Mind.Toolshed;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Components;
using Content.Trauma.Shared.JobListings;
using Content.Shared.Trigger.Systems;
using Content.Server.Power.EntitySystems;

namespace Content.Trauma.Server.JobListings;

/// <inheritdoc/>
public sealed partial class ScanalyzerSystem : SharedScanalyzerSystem
{
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private JobListingsSystem _jobs = default!;
    [Dependency] private TriggerSystem _trigger = default!;

    /// <summary>
    /// Determines if the inputted mind has scanned the grand theft item.
    /// </summary>
    public bool IsScanned(Entity<MindComponent> mind, ProtoId<StealTargetGroupPrototype> target)
    {
        if (!TryComp<ScanalyzerMindArchiveComponent>(mind.Owner, out var archive))
            return false;
        return archive.ScannedStealTargetGroups.Contains(target);
    }

    /// <summary>
    /// Register a grand theft item as scanned.
    /// </summary>
    public void RegisterScan(Entity<MindComponent> mind, ProtoId<StealTargetGroupPrototype> target)
    {
        var archive = EnsureComp<ScanalyzerMindArchiveComponent>(mind.Owner);
        if (!archive.ScannedStealTargetGroups.Contains(target))
            archive.ScannedStealTargetGroups.Add(target);
    }

    protected override void AfterScan(Entity<ScanalyzerComponent> ent, EntityUid user, ProtoId<StealTargetGroupPrototype> target)
    {
        if (!_mind.TryGetMind(user, out var mind, out var mindComp))
            return;
        RegisterScan((mind, mindComp), target);
        _jobs.UpdateUis((mind, mindComp));
    }

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<StealConditionRequireScanComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 0.0f;
        if (!TryComp<StealConditionComponent>(ent.Owner, out var stealComp))
            return;
        if (IsScanned((args.MindId, args.Mind), stealComp.StealGroup))
            args.Progress = 1.0f;
    }

    [SubscribeLocalEvent]
    private void OnScan(Entity<TriggerOnScanComponent> ent, ref ScanalyzerScanFinishedEvent args)
    {
        _trigger.Trigger(ent.Owner, args.User, ent.Comp.KeyOut, false);
    }
}

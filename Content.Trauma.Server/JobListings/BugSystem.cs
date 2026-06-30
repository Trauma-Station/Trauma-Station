// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Server.JobListings;

/// <inheritdoc/>
public sealed partial class BugSystem : SharedBugSystem
{
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private JobListingsSystem _jobs = default!;

    public override void Initialize()
    {
        base.Initialize();
        subscribeLocalEvent<BugComponent, UserAnchoredEvent>(OnWrench);
    }

    /// <summary>
    /// Register an area as bugged.
    /// </summary>
    public void RegisterBuggedArea(Entity<MindComponent> mind, EntProtoId area)
    {
        var archive = EnsureComp<BugMindArchiveComponent>(mind.Owner);
        if (!archive.BuggedAreas.Contains(target))
            archive.BuggedAreas.Add(target);
    }

    private void OnWrench(Entity<BugComponent> entity, ref UserAnchoredEvent args)
    {
        if (!Transform(entity.Owner).Anchored || !IsInCorrectArea(entity))
            return;
        if (!_mind.TryGetMind(user, out var mind, out var mindComp))
            return;

        RegisterBuggedArea((mind, mindComp), entity.Comp.TargetArea);
        _jobs.UpdateUis((mind, mindComp));
    }
}

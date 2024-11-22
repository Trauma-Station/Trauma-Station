// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Roles.Jobs;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Trauma.Shared.BloodCult;

namespace Content.Trauma.Server.BloodCult.Objectives;

public sealed partial class KillTargetCultSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private JobSystem _job = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    // TODO: make event for setting the cult's target incase they cryo or something
    [SubscribeLocalEvent]
    private void OnAfterAssign(Entity<KillTargetCultComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (GetTargetMind(args.Mind) is not {} mind)
            return;

        _metaData.SetEntityName(ent, GetTitle(mind, ent.Comp.Title), args.Meta);
    }

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<KillTargetCultComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = args.Mind.OwnedEntity is {} member && _cult.TargetKilled(member) ? 1f : 0f;
    }

    private string GetTitle(EntityUid target, LocId title)
    {
        var mind = Comp<MindComponent>(target);
        var targetName = mind.CharacterName;
        var jobName = _job.MindTryGetJobName(target);
        return Loc.GetString(title, ("targetName", targetName), ("job", jobName));
    }

    private EntityUid? GetTargetMind(MindComponent mindComp)
        => mindComp.OwnedEntity is {} mob && _cult.GetTarget(mob) is {} target
            ? _mind.GetMind(target)
            : null;

}

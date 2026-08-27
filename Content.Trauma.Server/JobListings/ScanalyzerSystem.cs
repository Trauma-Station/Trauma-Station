using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Server.JobListings;

public sealed partial class ScanalyzerSystem : SharedScanalyzerSystem
{
    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<StealConditionRequireScanComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 0.0f;
        if (!TryComp<StealConditionComponent>(ent.Owner, out var stealComp))
            return;
        if (IsScanned((args.MindId, args.Mind), stealComp.StealGroup))
            args.Progress = 1.0f;
    }

}
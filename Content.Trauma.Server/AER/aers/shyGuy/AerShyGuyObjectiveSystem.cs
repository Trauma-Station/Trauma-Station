// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Trauma.Shared.AER;

namespace Content.Trauma.Server.Aer.Objectives;

public sealed partial class AerShyGuyObjectiveSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AerShyGuyObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<AerShyGuyObjectiveComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.MindId, args.Mind);
    }

    private float GetProgress(EntityUid mindId, MindComponent mind)
    {
        //if there are no targets that saw 096
        if (TryComp<AerShyGuyComponent>(mind.OwnedEntity, out var shyGuy) && shyGuy.KillList.Count == 0)
            return 1f;
        else
            return 0f;
    }
}

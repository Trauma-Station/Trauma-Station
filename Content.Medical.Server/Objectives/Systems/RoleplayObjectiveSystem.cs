// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Medical.Server.Objectives.Systems;

public sealed class RoleplayObjectiveSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoleplayObjectiveComponent, ObjectiveGetProgressEvent>(OnRoleplayGetProgress);
    }

    private void OnRoleplayGetProgress(Entity<RoleplayObjectiveComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 1f;
    }
}

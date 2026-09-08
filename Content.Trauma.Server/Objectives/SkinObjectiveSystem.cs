// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Skinnable;
using Content.Server.Objectives.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Trauma.Server.Objectives;

public sealed partial class SkinObjectiveSystem : EntitySystem
{
    [Dependency] private TargetObjectiveSystem _target = default!;
    [Dependency] private EntityQuery<MindComponent> _mindQuery = default!;
    [Dependency] private EntityQuery<SkinnableComponent> _skinnableQuery = default!;

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<SkinObjectiveComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(ent.Owner, out var target))
            return;

        args.Progress = IsMobSkinned(target.Value) ? 1f : 0f;
    }

    public bool IsMobSkinned(EntityUid uid)
    {
        if (_mindQuery.CompOrNull(uid)?.OriginalOwnedEntity is { } mob)
            uid = GetEntity(mob);

        return !_skinnableQuery.TryComp(uid, out var comp) || comp.Skinned;
    }
}

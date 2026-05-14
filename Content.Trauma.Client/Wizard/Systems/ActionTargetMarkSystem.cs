// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking;
using Robust.Client.GameObjects;
using Content.Trauma.Common.Wizard;

namespace Content.Trauma.Client.Wizard.Systems;

public sealed partial class ActionTargetMarkSystem : CommonActionTargetMarkSystem
{
    [Dependency] private TransformSystem _transform = default!;

    private static readonly EntProtoId MarkProto = "ActionTargetMark";

    public override void SetMark(Entity<LockOnMarkActionComponent> ent, EntityUid? targetUid)
    {
        if (ent.Comp.Target == targetUid)
            return;

        ent.Comp.Target = targetUid;

        if (targetUid is not { } target)
        {
            QueueDel(ent.Comp.Mark);
            ent.Comp.Mark = null;
            return;
        }

        if (!TryComp(ent, out TransformComponent? xform))
            return;

        // If mark doesn't exist, spawn it; otherwise move it
        ent.Comp.Mark ??= SpawnAttachedTo(MarkProto, xform.Coordinates);

        var markXform = EnsureComp<TransformComponent>(ent.Comp.Mark.Value);
        _transform.SetCoordinates(ent.Comp.Mark.Value, markXform, xform.Coordinates);
        _transform.SetParent(ent.Comp.Mark.Value, markXform, target, xform);
    }
}

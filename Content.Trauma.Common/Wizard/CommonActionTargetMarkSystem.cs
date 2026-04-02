// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Wizard;

public abstract partial class CommonActionTargetMarkSystem : EntitySystem
{
    public abstract void SetMark(Entity<LockOnMarkActionComponent> ent, EntityUid? targetUid);
}

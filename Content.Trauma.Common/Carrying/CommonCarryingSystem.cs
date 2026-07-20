// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Carrying;

public abstract class CommonCarryingSystem : EntitySystem
{
    public abstract void DropCarried(EntityUid carrier, EntityUid carried);
}

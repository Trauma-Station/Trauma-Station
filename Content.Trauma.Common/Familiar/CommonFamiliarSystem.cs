// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Familiar;

public abstract class CommonFamiliarSystem : EntitySystem
{
    /// <summary>
    /// Make an entity a familiar serving a master.
    /// </summary>
    public abstract void SetMaster(EntityUid uid, EntityUid master);
}

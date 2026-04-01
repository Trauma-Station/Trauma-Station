// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Silicons.Laws;

public abstract partial class CommonSlavedBorgSystem : EntitySystem
{
    public abstract bool IsSlavedBorg(EntityUid uid);
}

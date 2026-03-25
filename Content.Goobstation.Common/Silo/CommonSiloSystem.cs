// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.Silo;

public abstract class CommonSiloSystem : EntitySystem
{
    public abstract EntityUid? GetSilo(EntityUid machine);
}

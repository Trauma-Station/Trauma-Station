// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.Silo;

public abstract partial class CommonSiloSystem : EntitySystem
{
    public abstract EntityUid? GetSilo(EntityUid machine);

    public abstract bool TryGetMaterialAmount(EntityUid machine, string material, out int amount);
    public abstract bool TryGetTotalMaterialAmount(EntityUid machine, out int amount);
    public abstract void DirtySilo(EntityUid machine);
}

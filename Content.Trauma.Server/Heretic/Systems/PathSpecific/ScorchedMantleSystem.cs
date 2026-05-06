// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Atmos.EntitySystems;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Ash;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed class ScorchedMantleSystem : SharedScorchedMantleSystem
{
    [Dependency] private readonly FlammableSystem _flammable = default!;

    protected override void UpdateFirestacks(EntityUid uid)
    {
        base.UpdateFirestacks(uid);

        _flammable.SetFireStacks(uid, 0.1f, ignite: true);
    }
}

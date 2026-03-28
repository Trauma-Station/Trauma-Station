// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Teleportation.Systems;
using Content.Shared.Teleportation;
using Content.Trauma.Shared.Heretic.Systems;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed class HereticBladeSystem : SharedHereticBladeSystem
{
    [Dependency] private readonly SharedRandomTeleportSystem _teleport = default!;

    protected override void RandomTeleport(EntityUid user, EntityUid blade, RandomTeleportComponent comp)
    {
        base.RandomTeleport(user, blade, comp);

        _teleport.RandomTeleport(user, comp, false);
        QueueDel(blade);
    }
}

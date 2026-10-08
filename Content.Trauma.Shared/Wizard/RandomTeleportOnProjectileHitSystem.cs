// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Projectiles;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Wizard.Components;
using Content.Trauma.Shared.Teleportation;
using Content.Trauma.Shared.Teleportation.Systems;

namespace Content.Trauma.Shared.Wizard;

public sealed partial class RandomTeleportOnProjectileHitSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private RandomTeleportSystem _teleport = default!;
    [Dependency] private EntityQuery<RandomTeleportComponent> _rtpQuery = default!;

    [SubscribeLocalEvent]
    private void OnHit(Entity<RandomTeleportOnProjectileHitComponent> ent, ref ProjectileHitEvent args)
    {
        var (uid, comp) = ent;
        if (_rtpQuery.TryComp(uid, out var rtp) && _whitelist.IsValid(comp.Whitelist, args.Target))
            _teleport.RandomTeleport(args.Target, (uid, rtp));
    }
}

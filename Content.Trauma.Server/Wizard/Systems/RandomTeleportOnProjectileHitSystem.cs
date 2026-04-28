// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Projectiles;
using Content.Shared.Whitelist;
using Content.Trauma.Server.Wizard.Components;
using Content.Trauma.Shared.Teleportation;
using Content.Trauma.Shared.Teleportation.Systems;

namespace Content.Trauma.Server.Wizard.Systems;

public sealed class RandomTeleportOnProjectileHitSystem : EntitySystem
{
    [Dependency] private readonly RandomTeleportSystem _teleport = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomTeleportOnProjectileHitComponent, ProjectileHitEvent>(OnHit);
    }

    private void OnHit(Entity<RandomTeleportOnProjectileHitComponent> ent, ref ProjectileHitEvent args)
    {
        var (uid, comp) = ent;
        if (TryComp(uid, out RandomTeleportComponent? tele) && _whitelist.IsValid(comp.Whitelist, args.Target))
            _teleport.RandomTeleport(args.Target, tele, predicted: false);
    }
}

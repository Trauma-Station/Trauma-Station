// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Wizard.SanguineStrike;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Wizard.Projectiles;

public sealed partial class LifeStealOnProjectileHitSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedSanguineStrikeSystem _sanguine = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LifeStealOnProjectileHitComponent, ProjectileHitEvent>(OnHit);
    }

    private void OnHit(Entity<LifeStealOnProjectileHitComponent> ent, ref ProjectileHitEvent args)
    {
        if (args.Shooter == null || args.Shooter.Value == args.Target)
            return;

        var (_, comp) = ent;

        if (!_whitelist.IsValid(comp.Whitelist, args.Target))
            return;

        if (_mobState.IsDead(args.Target))
            return;

        _sanguine.LifeSteal(args.Shooter.Value, comp.LifeStealAmount);
        List<EntityUid> target = new() { args.Target };
        _sanguine.BloodSteal(args.Shooter.Value, target, comp.BloodStealAmount, null);
        _sanguine.ParticleEffects(args.Shooter.Value, target, comp.Effect);
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;

namespace Content.Trauma.Shared.Collision.Blur;

public sealed partial class BlurOnCollideSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlurOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<BlurOnCollideComponent, ThrowDoHitEvent>(OnEntityHit);
    }

    private void OnEntityHit(Entity<BlurOnCollideComponent> ent, ref ThrowDoHitEvent args)
    {
        ApplyEffects(args.Target, ent.Comp);
    }

    private void OnProjectileHit(Entity<BlurOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        ApplyEffects(args.Target, ent.Comp);
    }

    private void ApplyEffects(EntityUid target, BlurOnCollideComponent component)
    {
        if (component.BlurTime > TimeSpan.Zero)
        {
            _statusEffects.TryAddStatusEffect<BlurryVisionComponent>(target,
                "BlurryVision",
                component.BlurTime,
                true);
        }

        if (component.BlindTime > TimeSpan.Zero)
        {
            _statusEffects.TryAddStatusEffect<TemporaryBlindnessComponent>(target,
                "TemporaryBlindness",
                component.BlindTime,
                true);
        }
    }
}

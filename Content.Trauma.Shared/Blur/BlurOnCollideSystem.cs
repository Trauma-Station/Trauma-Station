// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffectNew;
using Content.Shared.Throwing;

namespace Content.Trauma.Shared.Collision.Blur;

public sealed partial class BlurOnCollideSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _status = default!;

    private static readonly EntProtoId BlurryVision = "StatusEffectBlurryVision";

    [SubscribeLocalEvent]
    private void OnEntityHit(Entity<BlurOnCollideComponent> ent, ref ThrowDoHitEvent args)
    {
        ApplyEffects(args.Target, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnProjectileHit(Entity<BlurOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        ApplyEffects(args.Target, ent.Comp);
    }

    private void ApplyEffects(EntityUid target, BlurOnCollideComponent component)
    {
        if (component.BlurTime > TimeSpan.Zero)
            _status.TryUpdateStatusEffectDuration(target, BlurryVision, component.BlurTime);

        if (component.BlindTime > TimeSpan.Zero)
            _status.TryUpdateStatusEffectDuration(target, BlindnessSystem.BlindingStatusEffect, component.BlindTime);
    }
}

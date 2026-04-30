// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Trauma.Shared.Weapons.Melee;

public sealed class EffectsOnMeleeHitSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EffectsOnMeleeHitComponent, MeleeHitEvent>(OnHit);
    }

    private void OnHit(Entity<EffectsOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        var user = args.User;
        if (!ent.Comp.EffectForEveryHit)
        {
            var target = args.HitEntities[0];
            _effects.ApplyEffects(target, ent.Comp.TargetEffects!);
            _effects.ApplyEffects(user, ent.Comp.UserEffects!);

            return;
        }

        foreach (var target in args.HitEntities)
        {
            _effects.ApplyEffects(target, ent.Comp.TargetEffects!);
            _effects.ApplyEffects(user, ent.Comp.UserEffects!);
        }
    }
}

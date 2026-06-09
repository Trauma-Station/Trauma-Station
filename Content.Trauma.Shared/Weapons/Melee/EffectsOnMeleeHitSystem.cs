// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Trauma.Shared.Weapons.Melee;

public sealed partial class EffectsOnMeleeHitSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EffectsOnMeleeHitComponent, MeleeHitEvent>(OnHit);
    }

    private void OnHit(Entity<EffectsOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (args.HitEntities.Count == 0)
            return;

        var user = args.User;
        var targetEffects = ent.Comp.TargetEffects;
        var userEffects = ent.Comp.UserEffects;

        if (!ent.Comp.EffectForEveryHit)
        {
            var target = args.HitEntities[0];
            if (ent.Comp.TargetConditions is { } targetConds && !_conditions.TryConditions(target, targetConds))
                return;

            DoEffects(targetEffects, userEffects, user, target);
            return;
        }

        foreach (var target in args.HitEntities)
        {
            if (ent.Comp.TargetConditions is { } targetConds && !_conditions.TryConditions(target, targetConds))
                continue;

            DoEffects(targetEffects, userEffects, user, target);
        }
    }

    #region Helper

    /// <summary>
    /// Runs effects on the target and user.
    /// </summary>
    public void DoEffects(
        EntityEffect[]? targetEffects,
        EntityEffect[]? userEffects,
        EntityUid user,
        EntityUid target)
    {
        if (targetEffects is { } targetEffect)
            _effects.ApplyEffects(target, targetEffect);

        if (userEffects is { } userEffect)
            _effects.ApplyEffects(user, userEffect);
    }
    #endregion
}

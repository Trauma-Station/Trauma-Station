// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mobs.Systems;
using Content.Trauma.Shared.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

public sealed class ExperienceOnDamageSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExperienceOnDamageComponent, TookDamageEvent>(OnTookDamage);
    }

    private void OnTookDamage(Entity<ExperienceOnDamageComponent> ent, ref TookDamageEvent args)
    {
        if (!_mob.IsAlive(args.Target))
            return;

        var xp = Math.Min(args.Damage / ent.Comp.DamageScale, ent.Comp.MaxGain);
        // max level is capped by the damage taken in 1 hit
        // to get 100 toughness you have to take 100 damage over and over... have fun
        _knowledge.AddExperience(ent.Owner, args.Target, xp, limit: args.Damage);
    }
}

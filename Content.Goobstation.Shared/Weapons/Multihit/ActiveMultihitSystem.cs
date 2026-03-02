// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Goobstation.Shared.Weapons.Multihit;

public sealed class ActiveMultihitSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveMultihitComponent, MeleeHitEvent>(OnHit, after: new[] { typeof(MultihitSystem) });
    }

    private void OnHit(Entity<ActiveMultihitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        if (Math.Abs(ent.Comp.NextDamageMultiplier - 1f) < 0.01f)
            return;

        var modifierSet = new DamageModifierSet
        {
            Coefficients = args.BaseDamage.DamageDict
                .Select(x => new KeyValuePair<string, float>(x.Key, ent.Comp.NextDamageMultiplier))
                .ToDictionary(),
        };

        args.ModifiersList.Add(modifierSet);
    }
}

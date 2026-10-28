// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Trauma.Shared.Multihit;

public sealed partial class ActiveMultihitSystem : EntitySystem
{
    [SubscribeLocalEvent(after: new[] { typeof(MultihitSystem) })]
    private void OnHit(Entity<ActiveMultihitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        if (Math.Abs(ent.Comp.NextDamageMultiplier - 1f) < 0.01f)
            return;

        var modifierSet = new DamageModifierSet();
        foreach (var type in args.BaseDamage.DamageDict.Keys)
        {
            modifierSet.Coefficients[type] = ent.Comp.NextDamageMultiplier;
        }

        args.ModifiersList.Add(modifierSet);
    }
}

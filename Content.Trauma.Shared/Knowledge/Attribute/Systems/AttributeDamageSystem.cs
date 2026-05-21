// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute.Systems;

/// <summary>
/// Handles damage related stuff.
/// </summary>
public sealed partial class AttributeDamageSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, GetUserMeleeDamageEvent>(OnDamageGet);
    }

    private void OnDamageGet(Entity<KnowledgeHolderComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        var selfEv = new GetDamageModifierEvent();

        RaiseLocalEvent(ent.Owner, ref selfEv);

        var damage = new DamageModifierSet();

        foreach (var (key, _) in args.Damage.DamageDict)
        {
            damage.FlatReduction.Add(key, -selfEv.Mod); // Negative for more damage.
        }
        args.Modifiers.Add(damage);
    }
}

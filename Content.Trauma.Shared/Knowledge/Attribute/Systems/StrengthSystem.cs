// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Cuffs;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Attribute.Attribute.Components;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute.Systems;

/// <summary>
/// Handles all strength related bullshit.
/// </summary>
public sealed class StrengthSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, InstantUncuffEvent>(OnUncuff);
        SubscribeLocalEvent<KnowledgeHolderComponent, GetUserMeleeDamageEvent>(OnDamageGet);
    }

    private void OnUncuff(Entity<KnowledgeHolderComponent> ent, ref InstantUncuffEvent args)
    {
        var selfEv = new GetStrengthFeatEvent();
        var cuffsEv = new GetStrengthFeatEvent();

        RaiseLocalEvent(ent.Owner, ref selfEv);
        RaiseLocalEvent(args.Cuff, ref cuffsEv);

        var ev = new OnAttributeOpposedContest(args.Cuff, 20, selfEv.Mod, 20, cuffsEv.Mod);

        RaiseLocalEvent(ent, ref ev);
        if (ev.Failed)
        {
            var malus = EnsureComp<StrengthFeatTierdownComponent>(ent);
            malus.Mod += 2;
            _popup.PopupClient("You feel tired.", ent, ent, PopupType.Medium);
            // TODO: Add a grunt or extertion event. Should cause a voice thingy or stamina damage.
            return;
        }

        _popup.PopupClient("Holy shit, you broke free!", ent, ent, PopupType.Medium);

        args.CuffsBroken = true;
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

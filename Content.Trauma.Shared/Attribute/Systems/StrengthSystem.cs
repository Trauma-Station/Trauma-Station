// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Attribute;
using Content.Trauma.Common.Attribute.Components;
using Content.Trauma.Common.Cuffs;
using Content.Trauma.Shared.Attribute.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Attribute.Systems;

/// <summary>
/// Handles all strength related bullshit.
/// </summary>
public sealed class StrengthSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttributeHolderComponent, InstantUncuffEvent>(OnUncuff);
        SubscribeLocalEvent<AttributeHolderComponent, GetUserMeleeDamageEvent>(OnDamageGet);
    }

    private void OnUncuff(Entity<AttributeHolderComponent> ent, ref InstantUncuffEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

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
            _popup.PopupEntity("You feel tired.", ent, ent, PopupType.Medium);
            // TODO: Add a grunt or extertion event. Should cause a voice thingy or stamina damage.
            return;
        }

        args.CuffsBroken = true;
    }

    private void OnDamageGet(Entity<AttributeHolderComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

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

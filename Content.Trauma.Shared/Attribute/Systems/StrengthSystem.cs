// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Trauma.Common.Attribute;
using Content.Trauma.Common.Attribute.Components;
using Content.Trauma.Common.Cuffs;
using Content.Trauma.Shared.Attribute.Components;

namespace Content.Trauma.Shared.Attribute.Systems;

/// <summary>
/// Handles all strength related bullshit.
/// </summary>
public sealed class StrengthSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttributeHolderComponent, InstantUncuffEvent>(OnUncuff);
    }

    private void OnUncuff(Entity<AttributeHolderComponent> ent, ref InstantUncuffEvent args)
    {
        var selfEv = new GetStrengthFeatEvent();
        var cuffsEv = new GetStrengthFeatEvent();

        RaiseLocalEvent(ent.Owner, ref selfEv);
        RaiseLocalEvent(args.Cuff, ref cuffsEv);

        var ev = new OnAttributeOpposedContest(args.Cuff, 20, selfEv.Mod, 20, cuffsEv.Mod);

        RaiseLocalEvent(ent, ref ev);
        _popup.PopupEntity($"{ev.DiceUser}+{ev.ModUser} vs. {ev.DiceOpposed}+{ev.ModOpposed}", ent, ent, PopupType.Medium);
        if (ev.Failed)
        {
            var malus = EnsureComp<StrengthFeatTierdownComponent>(ent);
            malus.Mod += 1;
            _popup.PopupEntity("You feel tired.", ent, ent, PopupType.Medium);
            // TODO: Add a grunt or extertion event. Should cause a voice thingy or stamina damage.
            return;
        }

        args.CuffsBroken = true;
    }
}

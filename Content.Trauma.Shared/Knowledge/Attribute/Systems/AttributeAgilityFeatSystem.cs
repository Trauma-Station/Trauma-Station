// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Popups;
using Content.Shared.Slippery;
using Content.Shared.Stunnable;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute.Systems;

/// <summary>
/// Handles all agility feat related things.
/// </summary>
public sealed partial class AttributeAgilityFeatSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    private readonly int _slipThreshold = 15;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, SlipAttemptEvent>(OnSlip);
    }

    public void OnSlip(Entity<KnowledgeHolderComponent> ent, ref SlipAttemptEvent args)
    {
        if (HasComp<NoSlipComponent>(ent) || HasComp<KnockedDownComponent>(ent))
            return;

        var selfEv = new GetAgilityFeatEvent();

        RaiseLocalEvent(ent.Owner, ref selfEv);

        var ev = new SingleContestEvent(20, selfEv.Mod, _slipThreshold);

        RaiseLocalEvent(ent, ref ev);
        var threshold = _slipThreshold - (ev.DiceUser + selfEv.Mod);
        if (threshold > 0)
            return;

        _popup.PopupPredicted("You begin to slip, but some deft footwork manages to keep you upright.", ent, ent, PopupType.Medium);
        args.NoSlip = true;
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Popups;
using Content.Shared.Slippery;
using Content.Trauma.Common.Attribute;
using Content.Trauma.Common.Attribute.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Attribute.Systems;

/// <summary>
/// Handles all dexterity related bullshit.
/// </summary>
public sealed class DexteritySystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly int _slipThreshold = 15;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttributeHolderComponent, SlipAttemptEvent>(OnSlip);
    }

    public void OnSlip(Entity<AttributeHolderComponent> ent, ref SlipAttemptEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var selfEv = new GetAgilityFeatEvent();

        RaiseLocalEvent(ent.Owner, ref selfEv);

        var ev = new OnAttributeSingleContest(20, selfEv.Mod, _slipThreshold);

        RaiseLocalEvent(ent, ref ev);
        var threshold = _slipThreshold - (ev.DiceUser + selfEv.Mod);
        if (threshold > 5)
            return;
        else if (threshold > 0)
        {
            _popup.PopupEntity("You begin to slip, but you somehow manage to keep your balance.", ent, ent, PopupType.Medium);
            args.NoSlip = true;
            args.SlowOverSlippery = true;
            return;
        }

        _popup.PopupEntity("You begin to slip, but some deft footwork manages to keep you upright.", ent, ent, PopupType.Medium);
        args.NoSlip = true;
    }
}

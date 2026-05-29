// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Religion;
using Content.Server.Polymorph.Components;
using Content.Shared.Polymorph;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Void;
using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    protected override void SubscribeVoid()
    {
        base.SubscribeVoid();

        SubscribeLocalEvent<HereticVoidPrisonEvent>(OnVoidPrison);

        SubscribeLocalEvent<VoidPrisonComponent, PolymorphedEvent>(OnPrisonRevert);
    }

    private void OnPrisonRevert(Entity<VoidPrisonComponent> ent, ref PolymorphedEvent args)
    {
        if (!args.IsRevert)
            return;

        Spawn(ent.Comp.EndEffect, Transform(ent).Coordinates);
        Voidcurse.DoCurse(args.NewEntity);
    }

    private void OnVoidPrison(HereticVoidPrisonEvent args)
    {
        var target = args.Target;

        if (!HasComp<PolymorphableComponent>(target) || HasComp<VoidPrisonComponent>(target))
            return;

        if (!TryUseAbility(args))
            return;

        args.Handled = true;

        var ev = new BeforeCastTouchSpellEvent(target);
        RaiseLocalEvent(target, ev, true);
        if (ev.Cancelled)
            return;

        _poly.PolymorphEntity(target, args.Polymorph);
    }
}

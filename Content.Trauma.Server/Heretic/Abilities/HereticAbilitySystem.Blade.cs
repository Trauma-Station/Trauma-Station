// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Wounds;
using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    protected override void SubscribeBlade()
    {
        base.SubscribeBlade();

        SubscribeLocalEvent<HereticChampionStanceEvent>(OnChampionStance);
        SubscribeLocalEvent<EventHereticFuriousSteel>(OnFuriousSteel);
    }

    private void OnChampionStance(HereticChampionStanceEvent args)
    {
        foreach (var part in _body.GetOrgans<WoundableComponent>(args.Heretic))
        {
            part.Comp.CanRemove = args.Negative;
            Dirty(part);
        }
    }

    private void OnFuriousSteel(EventHereticFuriousSteel args)
    {
        if (!TryUseAbility(args))
            return;

        StatusNew.TryUpdateStatusEffectDuration(args.Performer, args.StatusEffect, out _, args.StatusDuration);
    }
}

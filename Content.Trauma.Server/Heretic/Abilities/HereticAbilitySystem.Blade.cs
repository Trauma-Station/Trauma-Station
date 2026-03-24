// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Wounds;
using Content.Shared.Heretic;
using Robust.Shared.Timing;

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

        var ent = args.Performer;

        _pblade.AddProtectiveBlade(ent);
        for (var i = 1; i < 3; i++)
        {
            // TODO kill timer
            Timer.Spawn(TimeSpan.FromSeconds(0.5f * i),
                () =>
                {
                    if (TerminatingOrDeleted(ent))
                        return;

                    _pblade.AddProtectiveBlade(ent);
                });
        }

        args.Handled = true;
    }
}

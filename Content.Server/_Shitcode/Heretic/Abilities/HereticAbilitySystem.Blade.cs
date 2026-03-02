// SPDX-FileCopyrightText: 2024 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 whateverusername0 <whateveremail>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <aviu00@protonmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 yglop <95057024+yglop@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Heretic;
using Robust.Shared.Timing;
using Content.Medical.Shared.Wounds; // Shitmed Change

namespace Content.Server.Heretic.Abilities;

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

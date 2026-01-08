// SPDX-FileCopyrightText: 2024 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Events;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Goobstation.Shared.Chemistry;

public sealed class HyposprayBlockNonMobInjectionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HyposprayBlockNonMobInjectionComponent, InjectorBeforeInjectEvent>(OnInjectAttempt);
    }

    private void OnInjectAttempt(Entity<HyposprayBlockNonMobInjectionComponent> ent, ref InjectorBeforeInjectEvent args)
    {
        if (!IsMob(args.TargetGettingInjected))
            args.Cancel();
    }

    private bool IsMob(EntityUid uid)
        => HasComp<MobStateComponent>(uid);
}

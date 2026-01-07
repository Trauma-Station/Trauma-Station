// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 yglop <95057024+yglop@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Random;

namespace Content.Goobstation.Server.WeaponRandomExplode;

public sealed class WeaponRandomExplodeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly SharedExplosionSystem _explosion = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeaponRandomExplodeComponent, ShotAttemptedEvent>(OnShot);
    }

    private void OnShot(EntityUid uid, WeaponRandomExplodeComponent component, ShotAttemptedEvent args)
    {
        if (component.ExplosionChance <= 0)
            return;

        var charge = _battery.GetCharge(uid);
        if (charge <= 0)
            return;

        // TODO: use predicted random and move this to shared
        if (!_random.Prob(component.ExplosionChance))
            return;

        var intensity = 1;
        if (component.MultiplyByCharge > 0)
        {
            intensity = Convert.ToInt32(component.MultiplyByCharge * (charge / 100));
        }

        _explosion.QueueExplosion(
            uid,
            typeId: "Default",
            totalIntensity: intensity,
            slope: 5,
            maxTileIntensity: 10);
        QueueDel(uid);
    }
}

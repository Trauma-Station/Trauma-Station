// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Weapons;

public sealed partial class WeaponRandomExplodeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedExplosionSystem _explosion = default!;

    [SubscribeLocalEvent]
    private void OnShot(Entity<WeaponRandomExplodeComponent> ent, ref ShotAttemptedEvent args)
    {
        var (uid, comp) = ent;
        if (comp.ExplosionChance <= 0)
            return;

        var (charge, _) = _battery.GetCharge(uid);
        if (charge <= 0)
            return;

        if (!SharedRandomExtensions.PredictedProb(_timing, comp.ExplosionChance, GetNetEntity(uid)))
            return;

        var intensity = 1;
        if (comp.MultiplyByCharge > 0)
        {
            intensity = (int) (comp.MultiplyByCharge * (charge / 100));
        }

        _explosion.QueueExplosion(
            uid,
            typeId: "Default",
            totalIntensity: intensity,
            slope: 5,
            maxTileIntensity: 10);
        PredictedQueueDel(uid);
        args.Cancel();
    }
}

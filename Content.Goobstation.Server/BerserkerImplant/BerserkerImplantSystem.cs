// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.BerserkerImplant;
using Content.Shared.Damage.Systems;
using Content.Trauma.Common.Wizard.Projectile;

namespace Content.Goobstation.Server.BerserkerImplant;

public sealed partial class BerserkerImplantSystem : SharedBerserkerImplantSystem
{
    [Dependency] private DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BerserkerImplantActiveComponent, ComponentRemove>(OnShutdown);
    }

    private void OnShutdown(Entity<BerserkerImplantActiveComponent> ent, ref ComponentRemove args)
    {
        _damageable.TryChangeDamage(ent.Owner, ent.Comp.DelayedDamage, true);
        RemComp<TrailComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = Timing.CurTime;
        var query = EntityQueryEnumerator<BerserkerImplantActiveComponent>();

        while (query.MoveNext(out var ent, out var berserker))
        {
            if (berserker.EndTime > curTime)
                continue;

            RemCompDeferred<BerserkerImplantActiveComponent>(ent);
        }
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Xenomorphs.Acid;
using Content.Trauma.Shared.Xenomorphs.Acid.Components;
using Content.Shared.Damage.Systems;

namespace Content.Trauma.Server.Xenomorphs.Acid;

public sealed partial class XenomorphAcidSystem : SharedXenomorphAcidSystem
{
    [Dependency] private DamageableSystem _damageable = default!;

    public override void Update(float frameTime)
    {
        var time = Timing.CurTime;

        var acidCorrodingQuery = EntityQueryEnumerator<AcidCorrodingComponent>();
        while (acidCorrodingQuery.MoveNext(out var uid, out var acidCorrodingComponent))
        {
            if (time > acidCorrodingComponent.NextDamageAt)
            {
                _damageable.TryChangeDamage(uid, acidCorrodingComponent.DamagePerSecond);
                acidCorrodingComponent.NextDamageAt = time + TimeSpan.FromSeconds(1);
            }

            if (time <= acidCorrodingComponent.AcidExpiresAt)
                continue;

            QueueDel(acidCorrodingComponent.Acid);
            RemCompDeferred<AcidCorrodingComponent>(uid);
        }
    }
}

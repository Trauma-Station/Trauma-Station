// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mobs.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Weapons.MissChance;

public sealed partial class MissChanceSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MissChanceComponent, PreventCollideEvent>(PreventCollide);
    }

    private void PreventCollide(Entity<MissChanceComponent> ent, ref PreventCollideEvent args)
    {
        var missChance = ent.Comp.Chance;
        if (args.Cancelled ||
            !HasComp<MobStateComponent>(args.OtherEntity) ||
            !SharedRandomExtensions.PredictedProb(_timing, missChance, GetNetEntity(ent)))
            return;

        args.Cancelled = true;
    }

    public void ApplyMissChance(EntityUid uid, float chance)
    {
        var missComp = EnsureComp<MissChanceComponent>(uid);
        missComp.Chance = chance;
        Dirty(uid, missComp);
    }
}

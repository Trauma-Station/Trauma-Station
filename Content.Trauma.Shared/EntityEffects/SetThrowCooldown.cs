// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class SetThrowCooldown : EntityEffectBase<SetThrowCooldown>
{
    [DataField(required: true)]
    public TimeSpan Cooldown;
}

public sealed partial class SetThrowCooldownEffectSystem : EntityEffectSystem<HandsComponent, SetThrowCooldown>
{
    [Dependency] private IGameTiming _timing = default!;

    protected override void Effect(Entity<HandsComponent> ent, ref EntityEffectEvent<SetThrowCooldown> args)
    {
        ent.Comp.NextThrowTime = _timing.CurTime + args.Effect.Cooldown;
        Dirty(ent);
    }
}

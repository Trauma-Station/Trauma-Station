// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Shared.StatusEffects;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class AccumulateCounterStatusEffect : EntityEffectBase<AccumulateCounterStatusEffect>
{
    [DataField(required: true)]
    public EntProtoId<CounterStatusEffectComponent> Status;

    [DataField(required: true)]
    public TimeSpan UpdateTime;

    [DataField]
    public int CounterDelta = 1;
}

public sealed partial class
    AccumulateCounterStatusEffectEffectSystem : EntityEffectSystem<TransformComponent,
    AccumulateCounterStatusEffect>
{
    [Dependency] private StatusEffectsSystem _status = default!;

    protected override void Effect(Entity<TransformComponent> ent,
        ref EntityEffectEvent<AccumulateCounterStatusEffect> args)
    {
        if (!_status.TryUpdateStatusEffectDuration(ent,
                args.Effect.Status,
                out var effect,
                args.Effect.UpdateTime))
            return;

        var counter = EnsureComp<CounterStatusEffectComponent>(effect.Value);
        counter.Count += args.Effect.CounterDelta;
        Dirty(effect.Value, counter);
    }
}

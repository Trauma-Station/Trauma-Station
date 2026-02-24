// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Throws the target entity in a random direction, with a fixed speed.
/// </summary>
public sealed partial class ThrowRandomly : EntityEffectBase<ThrowRandomly>
{
    [DataField]
    public float Speed = 10f;

    [DataField]
    public bool Predicted = true;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // not used by reagents idc
}

public sealed class ThrowRandomlyEffectSystem : EntityEffectSystem<MetaDataComponent, ThrowRandomly>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<ThrowRandomly> args)
    {
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent, ent.Comp).Id);
        var rand = new Random(seed);
        var angle = rand.NextAngle();
        var direction = angle.ToVec();

        var effect = args.Effect;
        _throwing.TryThrow(ent,
            direction,
            baseThrowSpeed: effect.Speed,
            user: args.User,
            predicted: effect.Predicted);
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Content.Shared.Throwing;
using Content.Trauma.Shared.EntityEffects.Throw;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Throws the target entity in a random direction, with a fixed speed.
/// </summary>
public sealed partial class ThrowRandomly : BaseThrowEntityEffect<ThrowRandomly>
{
    /// <summary>
    /// If not null, direction is multiplied with this variable.
    /// </summary>
    [DataField]
    public float? Distance;
};

public sealed partial class ThrowRandomlyEffectSystem : EntityEffectSystem<MetaDataComponent, ThrowRandomly>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<ThrowRandomly> args)
    {
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent, ent.Comp));
        var angle = rand.NextAngle();
        var direction = angle.ToVec();

        if (args.Effect.Distance is { } distance)
            direction *= distance;

        var effect = args.Effect;
        _throwing.TryThrow(ent,
            direction,
            baseThrowSpeed: effect.Speed,
            user: args.User,
            predicted: args.Predicted);
    }
}

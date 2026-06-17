// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Throwing;
using Content.Trauma.Shared.EntityEffects.Throw;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that throws the target entity at the direction they are currently facing.
/// </summary>
public sealed partial class ThrowAtFacingDirection : BaseThrowEntityEffect<ThrowAtFacingDirection>
{
    /// <summary>
    ///  How far to throw the target entity in that direction.
    /// </summary>
    [DataField]
    public float Distance = 5f;
}

public sealed partial class ThrowAtFacingDirectionEffectSystem : EntityEffectSystem<TransformComponent, ThrowAtFacingDirection>
{
    [Dependency] private ThrowingSystem _throwing = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<ThrowAtFacingDirection> args)
    {
        var xform = ent.Comp;
        var throwing = xform.LocalRotation.ToWorldVec() * args.Effect.Distance;
        var direction = xform.Coordinates.Offset(throwing);

        var speed = args.Effect.Speed;

        _throwing.TryThrow(
            ent,
            coordinates: direction,
            baseThrowSpeed: speed,
            user: args.User,
            predicted: args.Predicted);
    }
}

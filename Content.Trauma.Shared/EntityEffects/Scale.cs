// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Shared.Sprite;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Multiplies the target entity's sprite scale.
/// </summary>
public sealed partial class Scale : EntityEffectBase<Scale>
{
    /// <summary>
    /// What to multiply scale by, componentwise.
    /// Using 1 for an axis means it is left alone.
    /// </summary>
    [DataField(required: true)]
    public Vector2 Multiplier;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-scale-entity", ("chance", Probability), ("x", Multiplier.X), ("y", Multiplier.Y));
}

public sealed class ScaleEffectSystem : EntityEffectSystem<TransformComponent, Scale>
{
    [Dependency] private readonly SharedScaleVisualsSystem _scale = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<Scale> args)
    {
        var scale = _scale.GetSpriteScale(ent);
        var factors = args.Effect.Multiplier;
        _scale.SetSpriteScale(ent, Vector2.Multiply(scale, factors));
    }
}

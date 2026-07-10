// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Applies one set of effects if a condition is true, applies the other effects if not.
/// </summary>
public sealed partial class IfElse : EntityEffectBase<IfElse>
{
    /// <summary>
    /// Condition that must be true for <see cref="TrueEffects"/> to be applied.
    /// </summary>
    [DataField(required: true)]
    public EntityCondition Condition = default!;

    [DataField(required: true)]
    public EntityEffect[] TrueEffects = default!;

    [DataField(required: true)]
    public EntityEffect[] FalseEffects = default!;
}

public sealed partial class IfElseEffectSystem : EntityEffectSystem<TransformComponent, IfElse>
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<IfElse> args)
    {
        var e = args.Effect;
        var cond = _conditions.TryCondition(ent, e.Condition, user: args.User);
        var effects = cond ? e.TrueEffects : e.FalseEffects;
        _effects.ApplyEffects(ent, effects, args.Scale, args.User, args.Predicted);
    }
}

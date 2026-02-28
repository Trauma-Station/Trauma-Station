// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Multiplies the target entity's fixture radii.
/// Density is unchanged so mass will increase/decrease.
/// </summary>
public sealed partial class ScaleFixtures : EntityEffectBase<ScaleFixtures>
{
    /// <summary>
    /// What to scale fixtures by.
    /// </summary>
    [DataField(required: true)]
    public float Multiplier;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed class ScaleFixturesEffectSystem : EntityEffectSystem<FixturesComponent, ScaleFixtures>
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    protected override void Effect(Entity<FixturesComponent> ent, ref EntityEffectEvent<ScaleFixtures> args)
    {
        _physics.ScaleFixtures(ent.AsNullable(), args.Effect.Multiplier);
    }
}

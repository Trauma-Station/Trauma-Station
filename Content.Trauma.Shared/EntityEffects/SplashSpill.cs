// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Fluids;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Splash and spill a solution into a puddle.
/// </summary>
public sealed partial class SplashSpill : EntityEffectBase<SplashSpill>
{
    [DataField(required: true)]
    public Solution Solution = default!;

    /// <summary>
    /// Whether to play the splashing sound.
    /// </summary>
    [DataField]
    public bool Sound = true;
}

public sealed partial class SplashSpillEffectSystem : EntityEffectSystem<TransformComponent, SplashSpill>
{
    [Dependency] private SharedPuddleSystem _puddle = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<SplashSpill> args)
    {
        _puddle.TrySplashSpillAt(ent, ent.Comp.Coordinates,
            args.Effect.Solution,
            out _,
            args.Effect.Sound,
            args.User);
    }
}

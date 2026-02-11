// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class LegSweep : EntityEffectBase<LegSweep>
{
    /// <summary>
    /// The amount of time to force sleep for.
    /// </summary>
    [DataField(required: true)]
    public float Time = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null; // idc
}

public sealed class LegSweepEffectSystem : EntityEffectSystem<TransformComponent, LegSweep>
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<LegSweep> args)
    {
        if (args.User is { } user)
        {
            if (_standing.IsDown(user))
            {
                _standing.Stand(user);
                _statusEffects.TryAddStatusEffect(ent.Owner, "Knockdown", out _, TimeSpan.FromSeconds(args.Effect.Time * args.Scale * 2));
            }
            else
            {
                if (_random.Prob(Math.Max(0.5f * args.Scale, 1f)))
                    _statusEffects.TryAddStatusEffect(ent.Owner, "Knockdown", out _, TimeSpan.FromSeconds(args.Effect.Time * args.Scale));
            }
        }
    }
}

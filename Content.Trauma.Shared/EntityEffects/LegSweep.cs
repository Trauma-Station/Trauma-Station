using Content.Shared.EntityEffects;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class LegSweep : EntityEffectBase<LegSweep>
{
    /// <summary>
    /// The amount of time to knockdown for.
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
        if (args.User is not { } user)
            return;
        if (_standing.IsDown(user))
        {
            _standing.Stand(user);
            _statusEffects.TryAddStatusEffect(ent.Owner, "KnockedDown", out _, TimeSpan.FromSeconds(args.Effect.Time * args.Scale * 2));
        }
        else
        {
            if (_random.Prob(Math.Min(0.5f * args.Scale, 1f)))
                _statusEffects.TryAddStatusEffect(ent.Owner, "KnockedDown", out _, TimeSpan.FromSeconds(args.Effect.Time * args.Scale));
        }
    }
}

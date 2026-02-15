using Content.Shared.EntityEffects;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

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
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<LegSweep> args)
    {
        Log.Debug($"Applying leg sweep to {ent.Owner} with time {args.Effect.Time} and scale {args.Scale}. {ToPrettyString(args.User)} is {args.User is { }}");
        if (args.User is not { } user)
            return;

        var duration = TimeSpan.FromSeconds(args.Effect.Time * args.Scale);

        if (_standing.IsDown(user))
        {
            _standing.Stand(user);

            _stun.TryKnockdown(ent.Owner, duration * 2, true);
        }
        else
        {
            // Standard sweep chance
            if (_random.Prob(Math.Min(0.5f * args.Scale, 1f)))
            {
                _stun.TryKnockdown(ent.Owner, duration, true);
            }
        }
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

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
    [Dependency] private readonly IGameTiming _timing = default!;


    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<LegSweep> args)
    {
        if (args.User is not { } user)
            return;

        var duration = TimeSpan.FromSeconds(args.Effect.Time * args.Scale);

        if (_standing.IsDown(user))
        {
            _standing.Stand(user, force: true);
            _stun.ForceStandUp(user);
            _stun.TryKnockdown(ent.Owner, duration * 2, true);
        }
        else
        {
            // Standard sweep chance
            if (Random(user).NextFloat(0.0f, 1.0f) < Math.Min(0.5f * args.Scale, 1f))
            {
                _stun.TryKnockdown(ent.Owner, duration, true);
            }
        }
    }

    public System.Random Random(EntityUid uid)
    {
        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(uid).Id);
        return new System.Random(seed);
    }
}

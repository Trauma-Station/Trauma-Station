// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Common.MartialArts;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class BlockedBreathing : EntityEffectBase<BlockedBreathing>
{
    /// <summary>
    /// The amount of time to block breathing for.
    /// </summary>
    [DataField(required: true)]
    public float Time = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null; // idc
}

public sealed class BlockedBreathingEffectSystem : EntityEffectSystem<TransformComponent, BlockedBreathing>
{
    [Dependency] private readonly IGameTiming _timing = default!;


    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<BlockedBreathing> args)
    {
        var duration = _timing.CurTime + TimeSpan.FromSeconds(args.Effect.Time * args.Scale);

        var blockedBreathing = EnsureComp<BlockedBreathingComponent>(ent);
        blockedBreathing.BlockedTime = duration;
    }
}

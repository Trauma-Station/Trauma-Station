// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.StatusEffectNew;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class ForcedSleep : EntityEffectBase<ForcedSleep>
{
    /// <summary>
    /// The amount of time to force sleep for.
    /// </summary>
    [DataField(required: true)]
    public float Amount = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null; // idc
}

public sealed class ForcedSleepEffectSystem : EntityEffectSystem<TransformComponent, ForcedSleep>
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<ForcedSleep> args)
    {
        var time = args.Effect.Amount;
        if (args.User is { } user)
            _statusEffects.TryAddStatusEffect(user, "ForcedSleep", out _, TimeSpan.FromSeconds(time * args.Scale));
    }
}

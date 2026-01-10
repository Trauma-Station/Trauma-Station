// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Net.NetworkInformation;
using Content.Shared.EntityEffects;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class Knockdown : EntityEffectBase<Knockdown>
{
    /// <summary>
    /// The amount of time to force sleep for.
    /// </summary>
    [DataField(required: true)]
    public float Amount = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null; // idc
}

public sealed class KnockdownEffectSystem : EntityEffectSystem<TransformComponent, Knockdown>
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<Knockdown> args)
    {
        var time = args.Effect.Amount;
        if (args.User is { } user)
        {
            // Important for CQC
            if (_statusEffects.HasStatusEffect(user, "KnockedDown"))
                _statusEffects.TryAddStatusEffect(user, "ForcedSleep", out _, TimeSpan.FromSeconds(time * args.Scale));
            else
                _statusEffects.TryAddStatusEffect(user, "Knockdown", out _, TimeSpan.FromSeconds(time * args.Scale));
        }
    }
}

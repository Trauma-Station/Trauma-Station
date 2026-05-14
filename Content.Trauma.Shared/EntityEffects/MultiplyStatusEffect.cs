// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that multiplies the time of a status effect on the target
/// </summary>
public sealed partial class MultiplyStatusEffect : EntityEffectBase<MultiplyStatusEffect>
{
    /// <summary>
    /// The status effect we want to multiply it's time
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<StatusEffectComponent> EffectProto;

    /// <summary>
    /// How much to multiply the status effect's time
    /// </summary>
    [DataField]
    public float Amount;
}

public sealed class MultiplyStatusEffectEffectSystem : EntityEffectSystem<StatusEffectsComponent, MultiplyStatusEffect>
{
    protected override void Effect(Entity<StatusEffectsComponent> entity, ref EntityEffectEvent<MultiplyStatusEffect> args)
    {
        throw new NotImplementedException();
    }
}

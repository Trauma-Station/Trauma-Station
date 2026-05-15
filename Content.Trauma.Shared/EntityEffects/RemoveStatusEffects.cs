// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that completely removes status effects from an entity, if they exist.
/// </summary>
public sealed partial class RemoveStatusEffects : EntityEffectBase<RemoveStatusEffects>
{
    /// <summary>
    /// The status effects to remove from the target
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId<StatusEffectComponent>> StatusEffects;
}

public sealed class RemoveStatusEffectsEffectSystem : EntityEffectSystem<StatusEffectContainerComponent, RemoveStatusEffects>
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    protected override void Effect(Entity<StatusEffectContainerComponent> ent, ref EntityEffectEvent<RemoveStatusEffects> args)
    {
        var effects = args.Effect.StatusEffects;

        foreach (var effect in effects)
        {
            _statusEffects.TryRemoveStatusEffect(ent.Owner, effect);
        }
    }
}

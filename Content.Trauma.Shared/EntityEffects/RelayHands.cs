// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// For hands, applies an effect to the entities that the user is holding.
/// </summary>
public sealed partial class RelayHands : EntityEffectBase<RelayHands>
{
    /// <summary>
    /// The effect to apply
    /// </summary>
    [DataField(required: true)]
    public EntityEffect Effect = default!;
}

public sealed partial class RelayHandsEffectSystem : EntityEffectSystem<HandsComponent, RelayHands>
{
    [Dependency] private EffectDataSystem _data = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<HandsComponent> ent, ref EntityEffectEvent<RelayHands> args)
    {
        var effect = args.Effect;
        foreach (var item in _hands.EnumerateHeld(ent.AsNullable()))
        {
            _data.CopyData(ent, item);
            _effects.TryApplyEffect(item, effect.Effect, args.Scale, args.User, predicted: args.Predicted);
            _data.ClearData(item);
        }
    }
}

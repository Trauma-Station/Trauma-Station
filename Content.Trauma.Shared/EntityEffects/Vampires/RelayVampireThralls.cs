// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Vampires.Dantalion;

namespace Content.Trauma.Shared.EntityEffects.Vampires;

/// <summary>
/// Relays an effect to any nearby vampire thralls that the target owns.
/// </summary>
public sealed partial class RelayVampireThralls : EntityEffectBase<RelayVampireThralls>
{
    [DataField(required: true)]
    public EntityEffect Effect;

    /// <summary>
    /// The range of the lookup. If null, applies the effect to all thralls.
    /// </summary>
    [DataField]
    public float? Range;
}

public sealed partial class RelayVampireThrallsEffectSystem : EntityEffectSystem<VampireThrallsComponent, RelayVampireThralls>
{
    [Dependency] private EffectDataSystem _data = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<VampireThrallsComponent> ent, ref EntityEffectEvent<RelayVampireThralls> args)
    {
        var effect = args.Effect;
        var coords = _transform.GetMapCoordinates(ent.Owner);

        foreach (var thrall in ent.Comp.Thralls)
        {
            if (effect.Range is { } range)
            {
                var thrallCoords = _transform.GetMapCoordinates(thrall);
                if (!thrallCoords.InRange(coords, range))
                    continue;
            }

            _data.CopyData(ent, thrall);
            _effects.TryApplyEffect(thrall, effect.Effect, args.Scale, args.User, args.Predicted);
            _data.ClearData(thrall);
        }
    }
}

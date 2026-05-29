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
    public int? Range;
}

public sealed partial class RelayVampireThrallsEffectSystem : EntityEffectSystem<VampireThrallsComponent, RelayVampireThralls>
{
    [Dependency] private EffectDataSystem _data = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    private HashSet<Entity<VampireThrallComponent>> _thralls = new();

    protected override void Effect(Entity<VampireThrallsComponent> ent, ref EntityEffectEvent<RelayVampireThralls> args)
    {
        var effect = args.Effect;
        var xform = Transform(ent.Owner);

        if (effect.Range is { } range)
        {
            _thralls.Clear();
            _lookup.GetEntitiesInRange(xform.Coordinates, range, _thralls);
            foreach (var thrall in _thralls)
            {
                var uid = thrall.Owner;
                if (!ent.Comp.Thralls.Contains(thrall))
                    continue;

                _data.CopyData(ent, uid);
                _effects.TryApplyEffect(uid, effect.Effect, args.Scale, args.User);
                _data.ClearData(uid);
            }

            return;
        }

        // Range was null, so run the effect on all thralls
        foreach (var thrall in ent.Comp.Thralls)
        {
            _data.CopyData(ent, thrall);
            _effects.TryApplyEffect(thrall, effect.Effect, args.Scale, args.User);
            _data.ClearData(thrall);
        }
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Metabolism;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that sets the metabolizer type on the target
/// </summary>
public sealed partial class SetMetabolizerType : EntityEffectBase<SetMetabolizerType>
{
    /// <summary>
    /// The metabolizer types to set.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<MetabolizerTypePrototype>> MetabolizerTypes;
}

public sealed class SetMetabolizerTypeEffectSystem : EntityEffectSystem<MetabolizerComponent, SetMetabolizerType>
{
    protected override void Effect(Entity<MetabolizerComponent> ent, ref EntityEffectEvent<SetMetabolizerType> args)
    {
        var effect = args.Effect;

        ent.Comp.MetabolizerTypes = new();
        foreach (var metabolizer in effect.MetabolizerTypes)
        {
            ent.Comp.MetabolizerTypes.Add(metabolizer);
        }
        Dirty(ent);
    }
}

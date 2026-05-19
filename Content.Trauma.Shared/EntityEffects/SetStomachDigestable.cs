// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that sets the <see cref="StomachComponent.SpecialDigestible"/> of the target stomach.
/// </summary>
public sealed partial class SetStomachDigestable : EntityEffectBase<SetStomachDigestable>
{
    [DataField(required: true)]
    public EntityWhitelist SpecialDigestible = new();
}

public sealed class SetStomachDigestableEffectSystem : EntityEffectSystem<StomachComponent, SetStomachDigestable>
{
    protected override void Effect(Entity<StomachComponent> ent, ref EntityEffectEvent<SetStomachDigestable> args)
    {
        var effect = args.Effect;
        ent.Comp.SpecialDigestible = effect.SpecialDigestible;
        Dirty(ent);
    }
}

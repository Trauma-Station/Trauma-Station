// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Applies effects on user entity, if not null
/// </summary>
public sealed partial class RelayUser : EntityEffectBase<RelayUser>
{
    [DataField(required: true)]
    public EntityEffect[] Effects;
}

public sealed partial class RelayUserEffectSystem : EntityEffectSystem<MetaDataComponent, RelayUser>
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<RelayUser> args)
    {
        if (args.User is not { } user)
            return;

        _effects.ApplyEffects(user, args.Effect.Effects, args.Scale, user, args.Predicted);
    }
}

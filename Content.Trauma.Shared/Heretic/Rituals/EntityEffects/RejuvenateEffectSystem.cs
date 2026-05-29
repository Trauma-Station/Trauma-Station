// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Rejuvenate;

namespace Content.Trauma.Shared.Heretic.Rituals.EntityEffects;

public sealed class RejuvenateEffectSystem : EntityEffectSystem<MetaDataComponent, Rejuvenate>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Rejuvenate> args)
    {
        RaiseLocalEvent(entity, new RejuvenateEvent(args.Effect.Uncuff, args.Effect.ResetActions));
    }
}

public sealed partial class Rejuvenate : EntityEffectBase<Rejuvenate>
{
    [DataField]
    public bool ResetActions = true;

    [DataField]
    public bool Uncuff = true;
}

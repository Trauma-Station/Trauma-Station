// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffectOnUse;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Changes effects of `EntityEffectOnUseComponent`. Should be called at the end of `effectsToUsed` of `EntityEffectOnUse`.
/// </summary>
public sealed partial class ChangeEntityEffectOnUse : EntityEffectBase<ChangeEntityEffectOnUse>
{
    [DataField]
    public EntityEffect[]? EffectsToUser;

    [DataField]
    public EntityEffect[]? EffectsToUsed;
}

public sealed partial class ChangeEntityEffectOnUseEffectSystem : EntityEffectSystem<EntityEffectOnUseComponent, ChangeEntityEffectOnUse>
{
    protected override void Effect(Entity<EntityEffectOnUseComponent> ent, ref EntityEffectEvent<ChangeEntityEffectOnUse> args)
    {
        if (args.Effect.EffectsToUser != null)
            ent.Comp.EffectsToUser = args.Effect.EffectsToUser;

        if (args.Effect.EffectsToUsed != null)
            ent.Comp.EffectsToUsed = args.Effect.EffectsToUsed;
    }
}

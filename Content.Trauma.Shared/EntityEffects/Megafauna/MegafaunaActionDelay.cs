// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Shared.Megafauna.Components;
using Content.Lavaland.Shared.Megafauna.NumberSelectors;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects.Megafauna;

public sealed partial class MegafaunaActionDelay : EntityEffectBase<MegafaunaActionDelay>
{
    [DataField(required: true)]
    public MegafaunaNumberSelector ActionDelay;
}

public sealed partial class MegafaunaActionDelayEntityEffectSystem : EntityEffectSystem<MegafaunaAiComponent, MegafaunaActionDelay>
{
    protected override void Effect(Entity<MegafaunaAiComponent> ent, ref EntityEffectEvent<MegafaunaActionDelay> args)
    {
        ent.Comp.ActionDelaySelector = args.Effect.ActionDelay;
        Dirty(ent);
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Heretic.Rituals.EntityEffects;

public sealed class RaiseEventsEffectSystem : EntityEffectSystem<MetaDataComponent, RaiseEvents>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<RaiseEvents> args)
    {
        foreach (var ev in args.Effect.Events)
        {
            RaiseLocalEvent(entity, ev, true);
        }
    }
}
public sealed partial class RaiseEvents : EntityEffectBase<RaiseEvents>
{
    [DataField(required: true), NonSerialized]
    public object[] Events = default!;
}

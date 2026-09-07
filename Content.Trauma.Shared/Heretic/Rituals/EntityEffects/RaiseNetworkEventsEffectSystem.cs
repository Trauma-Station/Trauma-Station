// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Common.EntityEffects;

namespace Content.Trauma.Shared.Heretic.Rituals.EntityEffects;

public sealed partial class RaiseNetworkEventsEffectSystem : EntityEffectSystem<MetaDataComponent, RaiseNetworkEvents>
{
    [Dependency] private INetManager _net = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<RaiseNetworkEvents> args)
    {
        if (_net.IsClient)
            return;

        var netEnt = GetNetEntity(entity, entity.Comp);

        foreach (var ev in args.Effect.Events)
        {
            ev.Entity = netEnt;
            RaiseNetworkEvent(ev);
        }
    }
}
public sealed partial class RaiseNetworkEvents : EntityEffectBase<RaiseNetworkEvents>
{
    [DataField(required: true), NonSerialized]
    public EntityEffectNetworkEvent[] Events = default!;
}

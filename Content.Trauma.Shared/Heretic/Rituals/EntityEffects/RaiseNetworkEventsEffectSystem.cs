// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Common.EntityEffects;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.Heretic.Rituals.EntityEffects;

public sealed class RaiseNetworkEventsEffectSystem : EntityEffectSystem<MetaDataComponent, RaiseNetworkEvents>
{
    [Dependency] private INetManager _net = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<RaiseNetworkEvents> args)
    {
        if (_net.IsClient)
            return;

        var filter = Filter.Pvs(entity);
        if (args.Effect.SendToUser)
            filter = filter.RemoveWhereAttachedEntity(e => e == entity.Owner);

        foreach (var ev in args.Effect.Events)
        {
            ev.Entity = GetNetEntity(entity);
            RaiseNetworkEvent(ev, filter);
        }
    }
}
public sealed partial class RaiseNetworkEvents : EntityEffectBase<RaiseNetworkEvents>
{
    [DataField(required: true), NonSerialized]
    public EntityEffectNetworkEvent[] Events = default!;

    /// <summary>
    /// will this filter out user or not
    /// </summary>
    [DataField]
    public bool SendToUser = true;
}

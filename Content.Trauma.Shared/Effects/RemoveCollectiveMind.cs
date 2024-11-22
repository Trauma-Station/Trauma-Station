using Content.Shared.EntityEffects;
using Content.Trauma.Common.CollectiveMind;

namespace Content.Trauma.Shared.Effects;

/// <summary>
/// Removes a collective mind channel from the target entity.
/// </summary>
public sealed partial class RemoveCollectiveMind : EntityEffectBase<RemoveCollectiveMind>
{
    [DataField(required: true)]
    public ProtoId<CollectiveMindPrototype> Channel;
}

public sealed partial class RemoveCollectiveMindEffectSystem : EntityEffectSystem<CollectiveMindComponent, RemoveCollectiveMind>
{
    protected override void Effect(Entity<CollectiveMindComponent> ent, ref EntityEffectEvent<RemoveCollectiveMind> args)
    {
        ent.Comp.Channels.Remove(args.Effect.Channel);
        Dirty(ent);
    }
}

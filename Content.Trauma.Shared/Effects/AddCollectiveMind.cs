using Content.Shared.EntityEffects;
using Content.Trauma.Common.CollectiveMind;

namespace Content.Trauma.Shared.Effects;

/// <summary>
/// Adds a collective mind channel to the target entity.
/// </summary>
public sealed partial class AddCollectiveMind : EntityEffectBase<AddCollectiveMind>
{
    [DataField(required: true)]
    public ProtoId<CollectiveMindPrototype> Channel;
}

public sealed partial class AddCollectiveMindEffectSystem : EntityEffectSystem<CollectiveMindComponent, AddCollectiveMind>
{
    protected override void Effect(Entity<CollectiveMindComponent> ent, ref EntityEffectEvent<AddCollectiveMind> args)
    {
        ent.Comp.Channels.Add(args.Effect.Channel);
        Dirty(ent);
    }
}

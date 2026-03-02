using Content.Shared.EntityEffects;

namespace Content.Shared._Shitcode.Heretic.Rituals.EntityEffects;

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

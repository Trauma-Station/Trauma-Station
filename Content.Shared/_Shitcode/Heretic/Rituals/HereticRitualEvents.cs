using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;

namespace Content.Shared._Shitcode.Heretic.Rituals;

[ByRefEvent]
public readonly record struct HereticRitualEffectEvent<T>(T Effect, Entity<HereticRitualComponent> Ritual, EntityUid? User)
    where T : EntityEffectBase<T>
{
    public readonly T Effect = Effect;

    public readonly Entity<HereticRitualComponent> Ritual = Ritual;

    public readonly EntityUid? User = User;
}

[ByRefEvent]
public record struct HereticRitualConditionEvent<T>(T Condition, Entity<HereticRitualComponent> Ritual)
    where T : EntityConditionBase<T>
{
    [DataField]
    public bool Result;

    public readonly T Condition = Condition;

    public readonly Entity<HereticRitualComponent> Ritual = Ritual;
}

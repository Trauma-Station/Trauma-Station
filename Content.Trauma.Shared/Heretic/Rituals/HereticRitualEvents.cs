// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Heretic.Rituals;

[ByRefEvent]
public readonly record struct HereticRitualEffectEvent<T>(T Effect, Entity<HereticRitualRaiserComponent> Ritual, EntityUid? User, bool Predicted)
    where T : EntityEffectBase<T>
{
    public readonly T Effect = Effect;

    public readonly Entity<HereticRitualRaiserComponent> Ritual = Ritual;

    public readonly EntityUid? User = User;

    public readonly bool Predicted = Predicted;
}

[ByRefEvent]
public record struct HereticRitualConditionEvent<T>(T Condition, Entity<HereticRitualRaiserComponent> Ritual, EntityUid? user)
    where T : EntityConditionBase<T>
{
    public bool Result;

    public readonly T Condition = Condition;

    public readonly Entity<HereticRitualRaiserComponent> Ritual = Ritual;

    public readonly EntityUid? User = user;
}

[ByRefEvent]
public readonly record struct HereticRitualOwnerSetEvent(EntityUid Owner);

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Heretic.Rituals;

public sealed class HereticRitualEffectSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticRitualRaiserComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<HereticRitualRaiserComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Raiser = new HereticRitualRaiser(EntityManager, this, ent);
    }

    public void ApplyEffect(EntityUid target, EntityEffect effect, Entity<HereticRitualRaiserComponent> ritual, EntityUid? user)
    {
        effect.RaiseEvent(target, ritual.Comp.Raiser, 1f, user);
    }

    public bool TryApplyEffect(EntityUid target,
        EntityEffect effect,
        Entity<HereticRitualRaiserComponent> ritual,
        EntityUid? user)
    {
        if (!TryConditions(target, effect.Conditions, ritual))
            return false;

        ApplyEffect(target, effect, ritual, user);
        return true;
    }

    public void ApplyEffects(EntityUid target,
        EntityEffect[] effects,
        Entity<HereticRitualRaiserComponent> ritual,
        EntityUid? user)
    {
        foreach (var effect in effects)
        {
            TryApplyEffect(target, effect, ritual, user);
        }
    }

    public bool TryCondition(EntityUid uid, EntityCondition condition, Entity<HereticRitualRaiserComponent> ritual)
    {
        return condition.Inverted != condition.RaiseEvent(uid, ritual.Comp.Raiser);
    }

    public bool AnyCondition(EntityUid target, EntityCondition[]? conditions, Entity<HereticRitualRaiserComponent> ritual)
    {
        if (conditions == null)
            return true;

        foreach (var condition in conditions)
        {
            if (TryCondition(target, condition, ritual))
                return true;
        }

        return false;
    }

    public bool TryConditions(EntityUid target, EntityCondition[]? conditions, Entity<HereticRitualRaiserComponent> ritual)
    {
        if (conditions == null)
            return true;

        foreach (var condition in conditions)
        {
            if (!TryCondition(target, condition, ritual))
                return false;
        }

        return true;
    }

    public bool TryEffects(EntityUid target,
        IEnumerable<EntityEffect> effects,
        Entity<HereticRitualRaiserComponent> ritual,
        EntityUid? user)
    {
        foreach (var effect in effects)
        {
            if (!TryApplyEffect(target, effect, ritual, user))
                return false;
        }

        return true;
    }

    public void TryApplyEffect(EntityUid target,
        [ForbidLiteral] ProtoId<EntityEffectPrototype> id,
        Entity<HereticRitualRaiserComponent> ritual,
        EntityUid? user)
    {
        var proto = _proto.Index(id);
        if (TryConditions(target, proto.Conditions, ritual))
            ApplyEffects(target, proto.Effects, ritual, user);
    }
}

public sealed class HereticRitualRaiser(
    IEntityManager entMan,
    HereticRitualEffectSystem sys,
    Entity<HereticRitualRaiserComponent> ritual)
    : IEntityEffectRaiser, IEntityConditionRaiser
{
    public Entity<HereticRitualRaiserComponent> Ritual => ritual;
    public IEntityManager EntMan => entMan;

    public void RaiseEffectEvent<T>(EntityUid target, T effect, float scale, EntityUid? user)
        where T : EntityEffectBase<T>
    {
        if (effect is not IHereticRitualEntry)
        {
            var ev = new EntityEffectEvent<T>(effect, scale, user);
            entMan.EventBus.RaiseLocalEvent(target, ref ev);
            return;
        }

        var ritualEv = new HereticRitualEffectEvent<T>(effect, ritual, user);
        entMan.EventBus.RaiseLocalEvent(target, ref ritualEv);
    }

    public bool RaiseConditionEvent<T>(EntityUid target, T condition) where T : EntityConditionBase<T>
    {
        if (condition is not IHereticRitualEntry)
        {
            var ev = new EntityConditionEvent<T>(condition);
            entMan.EventBus.RaiseLocalEvent(target, ref ev);
            return ev.Result;
        }

        var ritualEv = new HereticRitualConditionEvent<T>(condition, ritual);
        entMan.EventBus.RaiseLocalEvent(target, ref ritualEv);
        return ritualEv.Result;
    }

    public IEnumerable<T> GetTargets<T>(string applyOn)
    {
        if (!ritual.Comp.Blackboard.TryGetValue(applyOn, out var result))
            yield break;

        switch (result)
        {
            case T newTarget:
                yield return newTarget;
                break;
            case IEnumerable<T> uids:
            {
                foreach (var uid in uids)
                {
                    yield return uid;
                }

                break;
            }
        }
    }

    public void SaveResult(string key, object result)
    {
        ritual.Comp.Blackboard[key] = result;
    }

    public bool TryConditions(EntityUid uid, EntityCondition[]? conditions)
    {
        return sys.TryConditions(uid, conditions, ritual);
    }
}

public interface IHereticRitualEntry;

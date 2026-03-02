using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;

namespace Content.Shared._Shitcode.Heretic.Rituals;

public sealed class HereticRitualEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticRitualComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<HereticRitualComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Raiser = new HereticRitualRaiser(EntityManager, this, ent);
    }

    public void ApplyEffect(EntityUid target, EntityEffect effect, Entity<HereticRitualComponent> ritual, EntityUid? user)
    {
        var ent = effect is IHereticRitualEntry ? ritual.Owner : target;
        effect.RaiseEvent(ent, ritual.Comp.Raiser, 1f, user);
    }

    public bool TryApplyEffect(EntityUid target,
        EntityEffect effect,
        Entity<HereticRitualComponent> ritual,
        EntityUid? user)
    {
        if (!TryConditions(target, effect.Conditions, ritual))
            return false;

        ApplyEffect(target, effect, ritual, user);
        return true;
    }

    public void ApplyEffects(EntityUid target,
        EntityEffect[] effects,
        Entity<HereticRitualComponent> ritual,
        EntityUid? user)
    {
        foreach (var effect in effects)
        {
            TryApplyEffect(target, effect, ritual, user);
        }
    }

    public bool TryCondition(EntityUid uid, EntityCondition condition, Entity<HereticRitualComponent> ritual)
    {
        return condition.Inverted != condition.RaiseEvent(uid, ritual.Comp.Raiser);
    }

    public bool AnyCondition(EntityUid target, EntityCondition[]? conditions, Entity<HereticRitualComponent> ritual)
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

    public bool TryConditions(EntityUid target, EntityCondition[]? conditions, Entity<HereticRitualComponent> ritual)
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
        Entity<HereticRitualComponent> ritual,
        EntityUid? user)
    {
        foreach (var effect in effects)
        {
            if (!TryApplyEffect(target, effect, ritual, user))
                return false;
        }

        return true;
    }
}

public sealed class HereticRitualRaiser(
    IEntityManager entMan,
    HereticRitualEffectSystem sys,
    Entity<HereticRitualComponent> ritual)
    : IEntityEffectRaiser, IEntityConditionRaiser
{
    public Entity<HereticRitualComponent> Ritual => ritual;

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

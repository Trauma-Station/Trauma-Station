using System.Linq;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Healing;
using Content.Medical.Shared.Pain;
using Content.Shared.Body;
using Content.Shared.Mobs;
using Content.Shared.Rejuvenate;

namespace Content.Medical.Shared.Consciousness;

public sealed partial class ConsciousnessSystem
{
    private void InitProcess()
    {
        SubscribeLocalEvent<ConsciousnessComponent, MobStateChangedEvent>(OnMobStateChanged);
        // To prevent people immediately falling down as rejuvenated
        SubscribeLocalEvent<ConsciousnessComponent, RejuvenateEvent>(OnRejuvenate, after: [typeof(BodySystem)]);
        SubscribeLocalEvent<ConsciousnessComponent, LifeStealHealEvent>(OnLifeStealHeal);
        SubscribeLocalEvent<ConsciousnessRequiredComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<ConsciousnessRequiredComponent, OrganGotRemovedEvent>(OnOrganRemoved);
        SubscribeLocalEvent<ConsciousnessComponent, MapInitEvent>(OnConsciousnessMapInit);
        SubscribeLocalEvent<ConsciousnessComponent, SuicideLethalDamageEvent>(OnSuicideLethalDamage);
        SubscribeLocalEvent<ConsciousnessComponent, ModifySelfHealSpeedEvent>(OnModifySelfHealSpeed);
    }

    private const string NerveSystemIdentifier = "nerveSystem";

    private void UpdatePassedOut(float frameTime)
    {
        var query = EntityQueryEnumerator<ConsciousnessComponent>();
        while (query.MoveNext(out var ent, out var consciousness))
        {
            if (consciousness.ForceDead
                || _timing.CurTime < consciousness.NextConsciousnessUpdate)
                continue;

            consciousness.NextConsciousnessUpdate = _timing.CurTime + consciousness.ConsciousnessUpdateTime;

            foreach (var modifier in consciousness.Modifiers.Where(modifier => modifier.Value.Time < _timing.CurTime))
                RemoveConsciousnessModifier(ent, modifier.Key.Item1, modifier.Key.Item2, consciousness);

            foreach (var multiplier in consciousness.Multipliers.Where(multiplier => multiplier.Value.Time < _timing.CurTime))
                RemoveConsciousnessMultiplier(ent, multiplier.Key.Item1, multiplier.Key.Item2, consciousness);

            if (consciousness.PassedOutTime < _timing.CurTime && consciousness.PassedOut)
            {
                consciousness.PassedOut = false;
                CheckConscious(ent, consciousness);
            }

            if (consciousness.ForceConsciousnessTime < _timing.CurTime && consciousness.ForceConscious)
            {
                consciousness.ForceConscious = false;
                CheckConscious(ent, consciousness);
            }
        }
    }

    private void OnMobStateChanged(Entity<ConsciousnessComponent> ent, ref MobStateChangedEvent args)
    {
        // let the brain know
        if (TryGetNerveSystem(ent, out var brain))
            RaiseLocalEvent(brain.Value, args);

        if (args.NewMobState != MobState.Dead)
            return;

        AddConsciousnessModifier(ent, ent, -ent.Comp.Cap, "DeathThreshold", ConsciousnessModType.Pain, consciousness: ent.Comp);
        // To prevent people from suddenly resurrecting while being dead. whoops

        foreach (var multiplier in
                 ent.Comp.Multipliers.Where(multiplier => multiplier.Value.Type != ConsciousnessModType.Pain))
            RemoveConsciousnessMultiplier(ent, multiplier.Key.Item1, multiplier.Key.Item2, ent.Comp);

        foreach (var modifier in
                 ent.Comp.Modifiers.Where(modifier => modifier.Value.Type != ConsciousnessModType.Pain))
            RemoveConsciousnessModifier(ent, modifier.Key.Item1, modifier.Key.Item2, ent.Comp);
    }

    private void OnRejuvenate(Entity<ConsciousnessComponent> ent, ref RejuvenateEvent args)
    {
        ClearPain(ent);
    }

    private void OnLifeStealHeal(Entity<ConsciousnessComponent> ent, ref LifeStealHealEvent args)
    {
        ClearPain(ent);
    }

    private void ClearPain(Entity<ConsciousnessComponent> ent)
    {
        // remove all pain modifiers and multipliers from the brain
        if (ent.Comp.NerveSystem is {} brain)
        {
            foreach (var painModifier in brain.Comp.Modifiers.Keys)
                _pain.TryRemovePainModifier(brain.Owner,
                    painModifier.Item1,
                    painModifier.Item2,
                    brain.Comp);

            foreach (var painMultiplier in brain.Comp.Multipliers.Keys)
                _pain.TryRemovePainMultiplier(brain.Owner, painMultiplier, brain.Comp);

            foreach (var nerve in brain.Comp.Nerves)
                foreach (var painFeelsModifier in nerve.Value.PainFeelingModifiers.Keys)
                    _pain.TryRemovePainFeelsModifier(painFeelsModifier.Item1, painFeelsModifier.Item2, nerve.Key, nerve.Value);
        }

        // remove consciousness multipliers/modifiers that aren't from traumas
        foreach (var multiplier in ent.Comp.Multipliers)
        {
            if (multiplier.Value.Type != ConsciousnessModType.Pain)
                continue;

            RemoveConsciousnessMultiplier(ent, multiplier.Key.Item1, multiplier.Key.Item2, ent.Comp);
        }

        foreach (var modifier in ent.Comp.Modifiers)
        {
            if (modifier.Value.Type != ConsciousnessModType.Pain)
                continue;

            RemoveConsciousnessModifier(ent, modifier.Key.Item1, modifier.Key.Item2, ent.Comp);
        }

        CheckRequiredParts(ent, ent.Comp);
        ForceConscious(ent, TimeSpan.FromSeconds(1f), ent.Comp);
    }

    private void OnConsciousnessMapInit(EntityUid uid, ConsciousnessComponent consciousness, MapInitEvent args)
    {
        if (consciousness.RawConsciousness < 0)
        {
            consciousness.RawConsciousness = consciousness.Cap;
            Dirty(uid, consciousness);
        }

        CheckConscious(uid, consciousness);
    }

    private void OnOrganInserted(EntityUid uid, ConsciousnessRequiredComponent component, ref OrganGotInsertedEvent args)
    {
        if (!_timing.IsFirstTimePredicted
            || !TryComp<ConsciousnessComponent>(args.Target, out var consciousness))
            return;

        if (consciousness.RequiredConsciousnessParts.TryGetValue(component.Identifier, out var value) && value.Item1 != null && value.Item1 != uid)
            Log.Warning($"ConsciousnessRequirementPart with duplicate Identifier {component.Identifier}:{uid} added to a body:" +
                             $" {args.Target} this will result in unexpected behaviour! Old {component.Identifier} wielder: {value.Item1}");

        consciousness.RequiredConsciousnessParts[component.Identifier] = (uid, component.CausesDeath, false);

        if (component.Identifier == NerveSystemIdentifier)
            consciousness.NerveSystem = (uid, Comp<NerveSystemComponent>(uid));

        CheckRequiredParts(args.Target, consciousness);
    }

    private void OnOrganRemoved(EntityUid uid, ConsciousnessRequiredComponent component, ref OrganGotRemovedEvent args)
    {
        if (!_timing.IsFirstTimePredicted
            || !TryComp<ConsciousnessComponent>(args.Target, out var consciousness))
            return;

        if (!consciousness.RequiredConsciousnessParts.TryGetValue(component.Identifier, out var value))
        {
            Log.Warning($"ConsciousnessRequirementPart with identifier {component.Identifier}:{uid} not found on body:{args.Target}");
            return;
        }

        consciousness.RequiredConsciousnessParts[component.Identifier] = (uid, value.Item2, true);
        CheckRequiredParts(args.Target, consciousness);
    }

    private void OnSuicideLethalDamage(Entity<ConsciousnessComponent> ent, ref SuicideLethalDamageEvent args)
    {
        foreach (var modifier in ent.Comp.Modifiers.Keys)
        {
            RemoveConsciousnessModifier(ent, modifier.Item1, modifier.Item2, ent.Comp);
        }

        foreach (var multiplier in ent.Comp.Multipliers.Keys)
        {
            RemoveConsciousnessMultiplier(ent, multiplier.Item1, multiplier.Item2, ent.Comp);
        }

        AddConsciousnessModifier(ent, ent, -ent.Comp.Cap, "Suicide", ConsciousnessModType.Pain, consciousness: ent.Comp);
        AddConsciousnessMultiplier(ent, ent, 0f, "Suicide", ConsciousnessModType.Pain, consciousness: ent.Comp);
    }

    private void OnModifySelfHealSpeed(Entity<ConsciousnessComponent> ent, ref ModifySelfHealSpeedEvent args)
    {
        args.Modifier *= (float) (ent.Comp.Threshold / ent.Comp.Cap - ent.Comp.Consciousness);
    }
}

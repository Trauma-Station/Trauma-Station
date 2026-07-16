// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Surgery;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Rejuvenate;
using Content.Trauma.Common.Damage;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Flesh;
using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Shared.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    private readonly HashSet<Entity<GhoulComponent>> _lookupGhouls = new();

    protected virtual void SubscribeFlesh()
    {
        SubscribeLocalEvent<HereticComponent, IncreaseFleshGhoulLimitEvent>(OnIncreaseFleshGhoulLimit);

        SubscribeLocalEvent<FleshPassiveComponent, OnHealthChangeEvent>(OnPoisonImmune);

        SubscribeLocalEvent<FleshSurgeryComponent, HeldRelayedEvent<SurgeryPainEvent>>(OnPain);
        SubscribeLocalEvent<FleshSurgeryComponent, HeldRelayedEvent<SurgeryIgnorePreviousStepsEvent>>(OnIgnore);
        SubscribeLocalEvent<FleshSurgeryComponent, TouchSpellUsedEvent>(OnTouchSpellUsed);
        SubscribeLocalEvent<FleshSurgeryComponent, UseInHandEvent>(OnFleshSurgeryUse);
    }

    private void OnIncreaseFleshGhoulLimit(Entity<HereticComponent> ent, ref IncreaseFleshGhoulLimitEvent args)
    {
        if (Heretic.TryGetRitual(ent.Owner, args.ImperfectRitual, out var ritual))
        {
            ritual.Value.Comp.Limit += args.VoicelessDeadLimitIncrease;
            Dirty(ritual.Value);
        }

        if (!TryComp(ent, out FleshHereticMindComponent? fleshMind))
            return;

        fleshMind.GhoulLimit += args.GhoulLimitIncrease;
        Dirty(ent, fleshMind);
    }

    private void OnPoisonImmune(Entity<FleshPassiveComponent> ent, ref OnHealthChangeEvent args)
    {
        foreach (var (key, value) in args.Damage.DamageDict)
        {
            if (!ent.Comp.HealthChangeImmuneDamageTypes.Contains(key))
                continue;

            if (value > FixedPoint2.Zero)
                args.Damage.DamageDict[key] = FixedPoint2.Zero;
        }
    }

    private void OnTouchSpellUsed(Entity<FleshSurgeryComponent> ent, ref TouchSpellUsedEvent args)
    {
        if (!TryComp(args.Target, out GhoulComponent? ghoul))
            return;
        args.Invoke = true;
        HealGhoul((args.Target, ghoul));
    }

    private void OnIgnore(Entity<FleshSurgeryComponent> ent, ref HeldRelayedEvent<SurgeryIgnorePreviousStepsEvent> args)
    {
        args.Args.Handled = true;
    }

    private void OnPain(Entity<FleshSurgeryComponent> ent, ref HeldRelayedEvent<SurgeryPainEvent> args)
    {
        args.Args.Cancelled = true;
    }

    private void HealGhoul(Entity<GhoulComponent> target)
    {
        var ev = new RejuvenateEvent(false, false);
        RaiseLocalEvent(target, ev);
        if (_mind.TryGetMind(target, out var mindId, out var mind))
            _mind.UnVisit(mindId, mind);
        // In case some organs were restored after rejuvenate
        if (target.Comp.DeathBehavior is not (GhoulDeathBehavior.Gib or GhoulDeathBehavior.GibOrgans))
            _ghoul.MakeOrgansFragile(target);
    }

    private void OnFleshSurgeryUse(Entity<FleshSurgeryComponent> ent, ref UseInHandEvent args)
    {
        if (!TryComp(ent, out TouchSpellComponent? touchSpell))
            return;

        if (!Heretic.TryGetHereticComponent(args.User, out var heretic, out _) ||
            heretic.CurrentPath != HereticPath.Flesh || !heretic.Ascended)
            return;

        var xform = Transform(args.User);
        var coords = _transform.GetMapCoordinates(args.User, xform);
        _lookupGhouls.Clear();
        Lookup.GetEntitiesInRange(coords, ent.Comp.AreaHealRange, _lookupGhouls, LookupFlags.Dynamic);
        foreach (var ghoul in _lookupGhouls)
        {
            HealGhoul(ghoul);
        }

        var cd = _grasp.CalculateAreaGraspCooldown((float) touchSpell.Cooldown.TotalSeconds,
            _lookupGhouls.Count,
            ent.Comp.AreaHealRange,
            1f);
        if (cd > ent.Comp.MaxAreaCooldown)
            cd = ent.Comp.MaxAreaCooldown;

        var effect = PredictedSpawnAtPosition(ent.Comp.KnitFleshEffect, xform.Coordinates);
        if (TryComp(effect, out Components.Side.AreaGraspEffectComponent? comp))
        {
            comp.SpawnTime = Timing.CurTime;
            Dirty(effect, comp);
        }

        _touchSpell.InvokeTouchSpell((ent.Owner, touchSpell), args.User, cd);
        args.Handled = true;
    }

    public virtual EntityUid? CreateFleshMimic(EntityUid uid,
        EntityUid user,
        int minionId,
        bool giveBlade,
        bool makeGhostRole,
        FixedPoint2 hp,
        EntityUid? hostile,
        bool hostileToUser)
    {
        return null;
    }
}

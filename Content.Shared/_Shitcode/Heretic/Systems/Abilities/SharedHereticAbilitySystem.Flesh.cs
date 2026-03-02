using Content.Medical.Common.Surgery;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Heretic;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Shared._Shitcode.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    private readonly HashSet<Entity<GhoulComponent>> _lookupGhouls = new();

    protected virtual void SubscribeFlesh()
    {
        SubscribeLocalEvent<EventHereticFleshSurgery>(OnFleshSurgery);
        SubscribeLocalEvent<EventHereticFleshSurgeryDoAfter>(OnFleshSurgeryDoAfter);

        SubscribeLocalEvent<FleshPassiveComponent, ImmuneToPoisonDamageEvent>(OnPoisonImmune);

        SubscribeLocalEvent<FleshSurgeryComponent, HeldRelayedEvent<SurgeryPainEvent>>(OnPain);
        SubscribeLocalEvent<FleshSurgeryComponent, HeldRelayedEvent<SurgeryIgnorePreviousStepsEvent>>(OnIgnore);
        SubscribeLocalEvent<FleshSurgeryComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<FleshSurgeryComponent, UseInHandEvent>(OnFleshSurgeryUse);
    }

    private void OnPoisonImmune(Entity<FleshPassiveComponent> ent, ref ImmuneToPoisonDamageEvent args)
    {
        args.Immune = true;
    }

    private void OnAfterInteract(Entity<FleshSurgeryComponent> ent, ref AfterInteractEvent args)
    {
        if (!HasComp<GhoulComponent>(args.Target))
            return;

        var dargs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.Delay,
            new EventHereticFleshSurgeryDoAfter(),
            args.User,
            args.Target,
            ent,
            showTo: EntityUid.Invalid)
        {
            Hidden = true, // Hidden because it also has health analyzer do-after
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnHandChange = false,
            BreakOnDropItem = false,
            Broadcast = true,
        };

        if (DoAfter.TryStartDoAfter(dargs))
            args.Handled = true;
    }

    private void OnIgnore(Entity<FleshSurgeryComponent> ent, ref HeldRelayedEvent<SurgeryIgnorePreviousStepsEvent> args)
    {
        args.Args.Handled = true;
    }

    private void OnPain(Entity<FleshSurgeryComponent> ent, ref HeldRelayedEvent<SurgeryPainEvent> args)
    {
        args.Args.Cancelled = true;
    }

    private void OnFleshSurgery(EventHereticFleshSurgery args)
    {
        var touch = GetTouchSpell<EventHereticFleshSurgery, FleshSurgeryComponent>(args.Performer, ref args);
        if (touch == null)
            return;

        EnsureComp<FleshSurgeryComponent>(touch.Value).Action = args.Action.Owner;
    }

    private void OnFleshSurgeryDoAfter(EventHereticFleshSurgeryDoAfter args)
    {
        if (args.Cancelled)
            return;

        if (args.Target is not { } target) // shouldn't really happen. just in case
            return;

        if (!TryComp(args.Used, out FleshSurgeryComponent? surgery))
            return;

        InvokeTouchSpell<FleshSurgeryComponent>((args.Used.Value, surgery), args.User);
        args.Handled = true;
        HealGhoul(target, args.User);
    }

    private void HealGhoul(EntityUid target, EntityUid user)
    {
        IHateWoundMed(target, null, null, null);
        if (TryComp(target, out MobStateComponent? mob))
            _mobState.ChangeMobState(target, MobState.Alive, mob, user);
        if (_mind.TryGetMind(target, out var mindId, out var mind))
            _mind.UnVisit(mindId, mind);
        RemComp<GhoulDeconvertComponent>(target);
    }

    private void OnFleshSurgeryUse(Entity<FleshSurgeryComponent> ent, ref UseInHandEvent args)
    {
        if (!Heretic.TryGetHereticComponent(args.User, out var heretic, out _) || heretic.CurrentPath != "Flesh" ||
            !heretic.Ascended)
            return;

        var xform = Transform(args.User);
        var coords = _transform.GetMapCoordinates(args.User, xform);
        _lookupGhouls.Clear();
        Lookup.GetEntitiesInRange(coords, ent.Comp.AreaHealRange, _lookupGhouls, LookupFlags.Dynamic);
        foreach (var ghoul in _lookupGhouls)
        {
            HealGhoul(ghoul, args.User);
        }

        var cd = _grasp.CalculateAreaGraspCooldown((float) ent.Comp.Cooldown.TotalSeconds,
            _lookupGhouls.Count,
            ent.Comp.AreaHealRange,
            1f);
        if (cd > ent.Comp.MaxAreaCooldown)
            cd = ent.Comp.MaxAreaCooldown;

        var effect = PredictedSpawnAtPosition(ent.Comp.KnitFleshEffect, xform.Coordinates);
        if (TryComp(effect, out AreaGraspEffectComponent? comp))
        {
            comp.SpawnTime = Timing.CurTime;
            Dirty(effect, comp);
        }

        InvokeTouchSpell(ent, args.User, cd);
        args.Handled = true;
    }
}

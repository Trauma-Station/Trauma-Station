// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.Traits;
using Content.Shared.Body;
using Content.Shared.Containers;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Pulling.Events;
using Content.Shared.Standing;

namespace Content.Medical.Shared.Body;

// TODO: prevent standing if your bones are broken
public sealed partial class LegsSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    private EntityQuery<LegsComponent> _query;
    private EntityQuery<MovementBodyPartComponent> _legQuery;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<LegsComponent>();
        _legQuery = GetEntityQuery<MovementBodyPartComponent>();

        SubscribeLocalEvent<LegsComponent, StandAttemptEvent>(OnStandAttempt);
        SubscribeLocalEvent<LegsComponent, AttemptStopPullingEvent>(OnAttemptStopPulling);

        SubscribeLocalEvent<MovementBodyPartComponent, OrganGotInsertedEvent>(OnLegAdded);
        SubscribeLocalEvent<MovementBodyPartComponent, OrganGotRemovedEvent>(OnLegRemoved);

        SubscribeLocalEvent<LegsStartParalyzedComponent, MapInitEvent>(OnParalyzedInit,
            after: [ typeof(ContainerFillSystem) ]); // run after the organs are added
    }

    private void OnStandAttempt(Entity<LegsComponent> ent, ref StandAttemptEvent args)
    {
        if (ent.Comp.Legs.Count < ent.Comp.Required)
            args.Cancel();
    }

    private void OnAttemptStopPulling(Entity<LegsComponent> ent, ref AttemptStopPullingEvent args) // Goobstation
    {
        if (args.User is not {} user || !Exists(user))
            return;

        if (user != ent.Owner)
            return;

        if (ent.Comp.Legs.Count > 0 || ent.Comp.Required == 0)
            return;

        args.Cancelled = true;
    }

    private void OnLegAdded(Entity<MovementBodyPartComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!_query.TryComp(args.Target, out var comp))
            return;

        comp.Legs.Add(ent.Owner);
        Dirty(args.Target, comp);

        UpdateMovementSpeed((args.Target, comp));
    }

    private void OnLegRemoved(Entity<MovementBodyPartComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Target) || !_query.TryComp(args.Target, out var comp))
            return;

        comp.Legs.Remove(ent.Owner);
        Dirty(args.Target, comp);

        UpdateMovementSpeed((args.Target, comp));
    }

    private void OnParalyzedInit(Entity<LegsStartParalyzedComponent> ent, ref MapInitEvent args)
    {
        if (!_query.TryComp(ent, out var legs))
            return;

        foreach (var leg in legs.Legs)
        {
            RemComp<MovementBodyPartComponent>(leg);
        }

        legs.Legs.Clear();
        Dirty(ent, legs);
        UpdateMovementSpeed((ent, legs));
    }

    public void UpdateMovementSpeed(Entity<LegsComponent> ent)
    {
        var walkSpeed = 0f;
        var sprintSpeed = 0f;
        var acceleration = 0f;
        foreach (var leg in ent.Comp.Legs)
        {
            if (!_legQuery.TryComp(leg, out var comp))
                continue;

            walkSpeed += comp.WalkSpeed;
            sprintSpeed += comp.SprintSpeed;
            acceleration += comp.Acceleration;
        }

        // missing a leg makes you move at half speed
        // somehow having 3+ legs makes you fast
        var scale = 1f / ent.Comp.Required;
        walkSpeed *= scale;
        sprintSpeed *= scale;
        acceleration *= scale;
        _movement.ChangeBaseSpeed(ent.Owner, walkSpeed, sprintSpeed, acceleration);
    }
}

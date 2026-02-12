// <Trauma>
using Content.Goobstation.Common.Body;
using Content.Medical.Common.Body;
using Content.Shared.Body;
using Robust.Shared.Timing;
// </Trauma>
using Content.Shared.Body.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Pointing;

namespace Content.Shared.Body.Systems;

public sealed class BrainSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    // </Trauma>
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainComponent, OrganGotInsertedEvent>(OnAdded); // Shitmed - actual event handler
        SubscribeLocalEvent<BrainComponent, OrganGotRemovedEvent>(OnRemoved); // Shitmed - actual event handler
        SubscribeLocalEvent<BrainComponent, PointAttemptEvent>(OnPointAttempt);
    }

    private void HandleMind(EntityUid newEntity, EntityUid oldEntity)
    {
        if (TerminatingOrDeleted(newEntity) || TerminatingOrDeleted(oldEntity))
            return;

        EnsureComp<MindContainerComponent>(newEntity);
        EnsureComp<MindContainerComponent>(oldEntity);

        var ghostOnMove = EnsureComp<GhostOnMoveComponent>(newEntity);
        ghostOnMove.MustBeDead = HasComp<MobStateComponent>(newEntity); // Don't ghost living players out of their bodies.

        if (!_mindSystem.TryGetMind(oldEntity, out var mindId, out var mind))
            return;

        _mindSystem.TransferTo(mindId, newEntity, mind: mind);
    }

    // <Shitmed> - do nothing for lings, use Active logic and don't do anything if a body already has a brain
    private void OnRemoved(EntityUid uid, BrainComponent brain, ref OrganGotRemovedEvent args)
    {
        // <Goob>
        var attemptEv = new BeforeBrainRemovedEvent();
        RaiseLocalEvent(args.Target, ref attemptEv);

        if (attemptEv.Blocked)
            return;
        // </Goob>

        brain.Active = false;
        Dirty(uid, brain);
        if (!HasBrain(args.Target))
        {
            // Prevents revival, should kill the user within a given timespan too.
            if (!TerminatingOrDeleted(args.Target) && !_timing.ApplyingState)
                EnsureComp<DebrainedComponent>(args.Target);
            HandleMind(uid, args.Target);
        }
    }

    private void OnAdded(EntityUid uid, BrainComponent brain, ref OrganGotInsertedEvent args)
    {
        // <Goob>
        var attemptEv = new BeforeBrainAddedEvent();
        RaiseLocalEvent(args.Target, ref attemptEv);

        if (attemptEv.Blocked)
            return;
        // </Goob>

        brain.Active = true;
        Dirty(uid, brain);
        if (HasBrain(args.Target))
        {
            RemComp<DebrainedComponent>(args.Target);
            HandleMind(args.Target, uid);
        }
    }

    private bool HasBrain(EntityUid entity)
    {
        if (!TryComp<BodyComponent>(entity, out var body))
            return false;

        if (HasComp<BrainComponent>(entity)) // sentient brain...
            return true;

        // TODO NUBODY: make this an event
        foreach (var brain in _body.GetOrgans<BrainComponent>((entity, body)))
        {
            if (brain.Comp.Active)
                return true;
        }

        return false;
    }
    // </Shitmed>

    private void OnPointAttempt(Entity<BrainComponent> ent, ref PointAttemptEvent args)
    {
        args.Cancel();
    }
}

// <Trauma>
using Content.Goobstation.Common.Changeling;
using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Body.Systems;
// </Trauma>
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Pointing;

namespace Content.Shared.Body.Systems;

public sealed class BrainSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private readonly SharedBodySystem _body = default!;
    // </Trauma>
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainComponent, OrganAddedToBodyEvent>(OnAdded); // Shitmed - actual event handler
        SubscribeLocalEvent<BrainComponent, OrganRemovedFromBodyEvent>(OnRemoved); // Shitmed - actual event handler
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
    private void OnRemoved(EntityUid uid, BrainComponent brain, ref OrganRemovedFromBodyEvent args)
    {
        if (HasComp<ChangelingComponent>(args.OldBody)) // no cheesing lings by removing brain
            return;

        brain.Active = false;
        Dirty(uid, brain);
        if (!HasBrain(args.OldBody))
        {
            // Prevents revival, should kill the user within a given timespan too.
            if (!TerminatingOrDeleted(args.OldBody))
                EnsureComp<DebrainedComponent>(args.OldBody);
            HandleMind(uid, args.OldBody);
        }
    }

    private void OnAdded(EntityUid uid, BrainComponent brain, ref OrganAddedToBodyEvent args)
    {
        if (HasComp<ChangelingComponent>(args.Body)) // no powergaming crew becoming lings
            return;

        brain.Active = true;
        Dirty(uid, brain);
        if (HasBrain(args.Body))
        {
            RemComp<DebrainedComponent>(args.Body);
            HandleMind(args.Body, uid);
        }
    }

    private bool HasBrain(EntityUid entity)
    {
        if (!TryComp<BodyComponent>(entity, out var body))
            return false;

        if (HasComp<BrainComponent>(entity)) // sentient brain...
            return true;

        foreach (var (organ, _) in _body.GetBodyOrgans(entity, body))
        {
            if (TryComp<BrainComponent>(organ, out var brain) && brain.Active)
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

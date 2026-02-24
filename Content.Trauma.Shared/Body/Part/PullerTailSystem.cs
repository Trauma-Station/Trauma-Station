// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Body.Part;

public sealed class PullerTailSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<HumanoidProfileComponent> _humanoidQuery;
    private EntityQuery<PullerComponent> _pullerQuery;

    public override void Initialize()
    {
        base.Initialize();

        _humanoidQuery = GetEntityQuery<HumanoidProfileComponent>();
        _pullerQuery = GetEntityQuery<PullerComponent>();

        SubscribeLocalEvent<PullerTailComponent, OrganGotInsertedEvent>(OnInserted);
        SubscribeLocalEvent<PullerTailComponent, OrganGotRemovedEvent>(OnRemoved);
    }

    private void OnInserted(Entity<PullerTailComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_timing.ApplyingState || !_pullerQuery.TryComp(args.Target, out var puller) || !puller.NeedsHands)
            return;

        if (ent.Comp.SpeciesWhitelist is {} whitelist &&
            !(_humanoidQuery.TryComp(args.Target, out var humanoid) &&
            whitelist.Contains(humanoid.Species)))
            return;

        puller.NeedsHands = false;
        Dirty(args.Target, puller);
        ent.Comp.Changed = true;
        Dirty(ent);
    }

    private void OnRemoved(Entity<PullerTailComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!ent.Comp.Changed || _timing.ApplyingState)
            return;

        ent.Comp.Changed = false;
        Dirty(ent);

        if (TerminatingOrDeleted(args.Target) || !_pullerQuery.TryComp(args.Target, out var puller))
            return;

        puller.NeedsHands = true;
        Dirty(args.Target, puller);
    }
}

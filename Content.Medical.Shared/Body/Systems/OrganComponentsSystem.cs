// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Shared.Body;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Surgery;

public sealed class OrganComponentsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganComponentsComponent, OrganGotInsertedEvent>(OnInserted);
        SubscribeLocalEvent<OrganComponentsComponent, OrganGotRemovedEvent>(OnRemoved);
    }

    private void OnInserted(Entity<OrganComponentsComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        // TODO SHITMED: this doesn't actually have any kind of refcounting :)
        if (ent.Comp.OnAdd is {} adding)
            EntityManager.AddComponents(args.Target, adding);
        if (ent.Comp.OnRemove is {} removing)
            EntityManager.RemoveComponents(args.Target, removing);
    }

    private void OnRemoved(Entity<OrganComponentsComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (_timing.ApplyingState || TerminatingOrDeleted(args.Target))
            return;

        if (ent.Comp.OnRemove is {} removed)
            EntityManager.AddComponents(args.Target, removed);
        if (ent.Comp.OnAdd is {} added)
            EntityManager.RemoveComponents(args.Target, added);
    }
}

using Content.Goobstation.Common.Body;
using Content.Shared.Whitelist;
using Content.Trauma.Common.Body.Part;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Body.Part;

public sealed class BodyPartCavitySystem : EntitySystem
{
    [Dependency] private readonly CommonInsideBodyPartSystem _insideBodyPart = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyPartCavityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BodyPartCavityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BodyPartCavityComponent, GetBodyPartCavityEvent>(OnGetBodyPartCavity);
        SubscribeLocalEvent<BodyPartCavityComponent, ContainerIsInsertingAttemptEvent>(OnInsertingAttempt);
        SubscribeLocalEvent<BodyPartCavityComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<BodyPartCavityComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnMapInit(Entity<BodyPartCavityComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.ContainerId);
    }

    private void OnShutdown(Entity<BodyPartCavityComponent> ent, ref ComponentShutdown args)
    {
        if (_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
            _container.ShutdownContainer(container);
    }

    private void OnGetBodyPartCavity(Entity<BodyPartCavityComponent> ent, ref GetBodyPartCavityEvent args)
    {
        if (args.Container != null)
            return;

        if (_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container) && container is ContainerSlot slot)
            args.Container = slot;
    }

    private void OnInsertingAttempt(Entity<BodyPartCavityComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (!_whitelist.CheckBoth(args.EntityUid, blacklist: ent.Comp.Blacklist, whitelist: ent.Comp.Whitelist))
            args.Cancel();
    }

    private void OnInserted(Entity<BodyPartCavityComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        // this will give a mob inserted into someones chest an action to burst out
        _insideBodyPart.InsertedIntoPart(args.Entity, ent);
    }

    private void OnRemoved(Entity<BodyPartCavityComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        // this will remove the chest burst action
        _insideBodyPart.RemovedFromPart(args.Entity);
    }
}

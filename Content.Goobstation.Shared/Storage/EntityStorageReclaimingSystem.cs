using Content.Shared.Storage.Components;
using Content.Shared.Materials;
using Robust.Shared.Containers;

namespace Content.Goobstation.Shared.Storage;

public sealed class EntityStorageReclaimingSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityStorageComponent, GotReclaimedEvent>(OnReclaimed);
    }

    private void OnReclaimed(Entity<EntityStorageComponent> ent, ref GotReclaimedEvent args)
    {
        if (ent.Comp.DeleteContentsOnDestruction)
            return;

        _container.EmptyContainer(ent.Comp.Contents, destination: args.ReclaimerCoordinates);
    }
}

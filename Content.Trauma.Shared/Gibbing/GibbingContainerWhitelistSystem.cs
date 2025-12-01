using Content.Shared.Body.Part;
using Content.Shared.Gibbing.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Trauma.Shared.Body.Part;

namespace Content.Trauma.Shared.Gibbing;

/// <summary>
/// Whitelists containers recursed by gibbing for throwing items.
/// </summary>
public sealed class GibbingContainerWhitelistSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandsComponent, AttemptEntityContentsGibEvent>(OnHandsGib);
        SubscribeLocalEvent<InventoryComponent, AttemptEntityContentsGibEvent>(OnInventoryGib);
        SubscribeLocalEvent<BodyPartComponent, AttemptEntityContentsGibEvent>(OnPartGib);
        SubscribeLocalEvent<BodyPartCavityComponent, AttemptEntityContentsGibEvent>(OnCavityGib);
    }

    private void OnHandsGib(Entity<HandsComponent> ent, ref AttemptEntityContentsGibEvent args)
    {
        var containers = Containers(ref args);
        foreach (var id in ent.Comp.Hands.Keys)
        {
            containers.Add(id);
        }
    }

    private void OnInventoryGib(Entity<InventoryComponent> ent, ref AttemptEntityContentsGibEvent args)
    {
        var containers = Containers(ref args);
        var slots = _inventory.GetSlotEnumerator(ent.AsNullable());
        while (slots.MoveNext(out var container))
        {
            if (container.ContainedEntity != null)
                containers.Add(container.ID);
        }
    }

    private void OnPartGib(Entity<BodyPartComponent> ent, ref AttemptEntityContentsGibEvent args)
    {
        // don't try throw solutions etc
        Containers(ref args);
    }

    private void OnCavityGib(Entity<BodyPartCavityComponent> ent, ref AttemptEntityContentsGibEvent args)
    {
        var containers = Containers(ref args);
        containers.Add(ent.Comp.ContainerId);
    }

    private List<string> Containers(ref AttemptEntityContentsGibEvent args)
        => args.AllowedContainers ??= new();
}

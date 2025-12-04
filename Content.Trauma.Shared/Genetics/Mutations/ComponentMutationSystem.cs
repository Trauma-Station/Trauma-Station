// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Shared.Genetics.Mutations;

public sealed class ComponentMutationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ComponentMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<ComponentMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<ComponentMutationComponent> ent, ref MutationAddedEvent args)
    {
        if (ent.Comp.Added is {} added)
            EntityManager.AddComponents(args.Target, added);
        if (ent.Comp.Removed is {} removed)
            EntityManager.RemoveComponents(args.Target, removed);
    }

    private void OnRemoved(Entity<ComponentMutationComponent> ent, ref MutationRemovedEvent args)
    {
        // removed components get readded first incase that mattered
        if (ent.Comp.Removed is {} removed)
            EntityManager.AddComponents(args.Target, removed);
        if (ent.Comp.Added is {} added)
            EntityManager.RemoveComponents(args.Target, added);
    }
}

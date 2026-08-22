// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Body.Chips;

public sealed class OrganChipComponentsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganChipComponentsComponent, OrganChipInsertedEvent>(OnInserted);
        SubscribeLocalEvent<OrganChipComponentsComponent, OrganChipRemovedEvent>(OnRemoved);
    }

    private void OnInserted(Entity<OrganChipComponentsComponent> ent, ref OrganChipInsertedEvent args)
    {
        if (args.Body is not { } body)
            return;

        // TODO: refcounting
        if (ent.Comp.OnAdd is {} adding)
            EntityManager.AddComponents(body, adding);
        if (ent.Comp.OnRemove is {} removing)
            EntityManager.RemoveComponents(body, removing);
    }

    private void OnRemoved(Entity<OrganChipComponentsComponent> ent, ref OrganChipRemovedEvent args)
    {
        if (args.Body is not { } body)
            return;

        if (ent.Comp.OnRemove is {} removed)
            EntityManager.AddComponents(body, removed);
        if (ent.Comp.OnAdd is {} added)
            EntityManager.RemoveComponents(body, added);
    }
}

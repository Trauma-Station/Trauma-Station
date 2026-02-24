// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Tag;

namespace Content.Trauma.Shared.Genetics.Mutations;

public sealed class TagMutationSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TagMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<TagMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<TagMutationComponent> ent, ref MutationAddedEvent args)
    {
        _tag.AddTags(ent, ent.Comp.Added);
        _tag.RemoveTags(ent, ent.Comp.Removed);
    }

    private void OnRemoved(Entity<TagMutationComponent> ent, ref MutationRemovedEvent args)
    {
        _tag.AddTags(ent, ent.Comp.Removed);
        _tag.RemoveTags(ent, ent.Comp.Added);
    }
}

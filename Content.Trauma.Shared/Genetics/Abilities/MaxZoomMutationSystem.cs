// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.Genetics.Abilties;

public sealed class MaxZoomMutationSystem : EntitySystem
{
    [Dependency] private readonly SharedContentEyeSystem _eye = default!;

    private EntityQuery<ContentEyeComponent> _query;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<ContentEyeComponent>();

        SubscribeLocalEvent<MaxZoomMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<MaxZoomMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<MaxZoomMutationComponent> ent, ref MutationAddedEvent args)
    {
        if (!_query.TryComp(args.Target, out var eye))
            return;

        _eye.SetMaxZoom(args.Target, eye.MaxZoom * ent.Comp.Modifier, eye);
    }

    private void OnRemoved(Entity<MaxZoomMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (!_query.TryComp(args.Target, out var eye))
            return;

        _eye.SetMaxZoom(args.Target, eye.MaxZoom / ent.Comp.Modifier, eye);
    }
}

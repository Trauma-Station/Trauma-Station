// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.Genetics.Abilties;

public sealed partial class MaxZoomMutationSystem : EntitySystem
{
    [Dependency] private SharedContentEyeSystem _eye = default!;
    [Dependency] private EntityQuery<ContentEyeComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

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

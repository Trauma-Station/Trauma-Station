// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction.Events;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Interaction;

/// <summary>
/// Prevents interaction with 2 entities that are inside of different containers.
/// This also includes drag-drop stuff.
/// </summary>
public sealed partial class ContainerInteractionSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TransformComponent, InteractionAttemptEvent>(OnInteractionAttempt);
    }

    private void OnInteractionAttempt(Entity<TransformComponent> ent, ref InteractionAttemptEvent args)
    {
        if (args.Cancelled || args.Target is not {} target)
            return;

        _container.TryGetContainingContainer((ent, ent.Comp, null), out var ourContainer);
        _container.TryGetContainingContainer(target, out var theirContainer);
        // allow the same container, neither of them being inside containers or we are not in a container
        if (ourContainer == theirContainer || ourContainer == null)
            return;

        // if they are different containers, allow it if one entity is contained by the other, directly or indirectly
        args.Cancelled = !_transform.ContainsEntity(ent, target) && !_transform.ContainsEntity(target, ent.AsNullable());
    }
}

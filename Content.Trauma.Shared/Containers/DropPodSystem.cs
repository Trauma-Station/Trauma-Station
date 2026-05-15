// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Containers;
using Robust.Shared.Spawners;

namespace Content.Trauma.Shared.Containers;

public sealed partial class DropPodSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    public static readonly EntProtoId DropPod = "DropPodPlayer";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DropPodComponent, TimedDespawnEvent>(OnTimedDespawn);
    }

    private void OnTimedDespawn(Entity<DropPodComponent> ent, ref TimedDespawnEvent args)
    {
        var container = _container.GetContainer(ent, ent.Comp.Container);
        // wouldn't be very nice if you spawned in then just got deleted because of some attempt event, would it
        _container.EmptyContainer(container, force: true);
    }

    /// <summary>
    /// Spawns a drop pod at the target entity and inserts it into the pod.
    /// </summary>
    public void MakeDropPod(EntityUid target)
    {
        var coords = Transform(target).Coordinates;
        var pod = Spawn(DropPod, coords);
        var containerName = Comp<DropPodComponent>(pod).Container;
        var container = _container.GetContainer(pod, containerName);
        _container.Insert(target, container);
    }
}

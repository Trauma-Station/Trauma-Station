// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Factory.Filters;
using Content.Shared.DeviceLinking;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Factory;

public sealed partial class StorageBinSystem : EntitySystem
{
    [Dependency] private AutomationFilterSystem _filter = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedDeviceLinkSystem _device = default!;

    public const string ContainerId = "storagebase";

    [SubscribeLocalEvent]
    private void OnInsertAttempt(Entity<StorageBinComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID != ContainerId || _timing.ApplyingState)
            return;

        if (_filter.IsBlocked(_filter.GetSlot(ent), args.EntityUid))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnEntInserted(Entity<StorageBinComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ContainerId)
            return;

        _device.InvokePort(ent.Owner, ent.Comp.InsertedPort);
    }

    [SubscribeLocalEvent]
    private void OnEntRemoved(Entity<StorageBinComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ContainerId)
            return;

        _device.InvokePort(ent.Owner, ent.Comp.RemovedPort);
    }
}

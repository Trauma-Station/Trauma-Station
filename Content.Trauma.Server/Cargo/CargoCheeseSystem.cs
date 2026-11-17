// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Cargo.Components;
using Robust.Shared.Containers;

namespace Content.Trauma.Server.Cargo;

/// <summary>
/// Prevents bypassing sale blacklists by putting items in a crate bag etc.
/// Makes the container also blacklisted, refcounted by each item to avoid exploits.
/// </summary>
public sealed partial class CargoCheeseSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private EntityQuery<CargoSellBlacklistComponent> _query = default!;

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CargoSellBlacklistComponent> ent, ref ComponentShutdown args)
    {
        if (!_container.TryGetContainingContainer(ent.Owner, out var container))
            return;

        Decrement(container.Owner);
    }

    [SubscribeLocalEvent]
    private void OnInsertedIntoContainer(Entity<CargoSellBlacklistComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        Increment(args.Container.Owner);
    }

    [SubscribeLocalEvent]
    private void OnRemovedFromContainer(Entity<CargoSellBlacklistComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        Decrement(args.Container.Owner);
    }

    private void Increment(EntityUid uid)
    {
        // if it already existed and has no blacklisted children it has to be intrinsically blacklisted
        // don't touch it to avoid exploits
        if (EnsureComp<CargoSellBlacklistComponent>(uid, out var comp) &&
            comp.BlacklistedChildren == 0)
        {
            return;
        }

        comp.BlacklistedChildren++;
    }

    private void Decrement(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid) ||
            !_query.TryComp(uid, out var comp))
            return;

        if (comp.BlacklistedChildren == 0)
            return; // no blacklist laundering

        if (--comp.BlacklistedChildren == 0)
            RemComp(uid, comp);
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Robust.Shared.Containers;

namespace Content.Goobstation.Shared.IdentityManagement;

/// <summary>
/// Updates your identity when you toggle a mask up or down.
/// </summary>
public sealed partial class IdentityBlockerToggleSystem : EntitySystem
{
    [Dependency] private IdentitySystem _identity = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private EntityQuery<InventoryComponent> _invQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdentityBlockerComponent, ComponentInit>(BlockerUpdateIdentity);
        SubscribeLocalEvent<IdentityBlockerComponent, ComponentRemove>(BlockerUpdateIdentity);
    }

    private void BlockerUpdateIdentity(EntityUid uid, IdentityBlockerComponent component, EntityEventArgs args)
    {
        var target = uid;

        if (_container.TryGetContainingContainer(uid, out var container) && _invQuery.HasComp(container.Owner))
            target = container.Owner;

        _identity.QueueIdentityUpdate(target);
    }
}

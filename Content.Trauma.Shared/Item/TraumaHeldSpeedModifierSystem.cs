// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Item;

/// <summary>
/// Reacts to <see cref="HeldSpeedModifierComponent"/> being added/removed while held/worn.
/// </summary>
public sealed partial class TraumaHeldSpeedModifierSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _moveSpeed = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private EntityQuery<InventoryComponent> _inventoryQuery = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<HeldSpeedModifierComponent> ent, ref ComponentStartup args)
    {
        RefreshHolderModifiers(ent);
    }

    [SubscribeLocalEvent]
    private void OnRemove(Entity<HeldSpeedModifierComponent> ent, ref ComponentRemove args)
    {
        RefreshHolderModifiers(ent);
    }

    private void RefreshHolderModifiers(EntityUid uid)
    {
        if (_container.TryGetContainingContainer(uid, out var container) &&
            _inventoryQuery.HasComp(container.Owner))
        {
            _moveSpeed.RefreshMovementSpeedModifiers(container.Owner);
        }
    }
}

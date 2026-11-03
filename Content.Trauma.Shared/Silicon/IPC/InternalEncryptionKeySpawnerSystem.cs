// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers;
using Content.Shared.Radio.Components;
using Content.Shared.Roles;
using Content.Trauma.Common.Inventory;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Silicon.IPC;

public sealed partial class InternalEncryptionKeySpawnerSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    private static readonly ProtoId<InventorySlotPrototype> Slot = "ears";
    private CompName _fillName;

    public override void Initialize()
    {
        base.Initialize();

        _fillName = Factory.CompName<ContainerFillComponent>();
    }

    [SubscribeLocalEvent]
    public void OnStartingGearEquipped(Entity<EncryptionKeyHolderComponent> ent, ref StartingGearEquippedEvent ev)
    {
        TryInsertEncryptionKey(ent, ev.StartingGear);
    }

    /// <summary>
    /// Inserts an IPC's encryption key from starting gear headset.
    /// </summary>
    /// <remarks>
    /// Doesn't support a profile's loadouts, have fun.
    /// </remarks>
    public void TryInsertEncryptionKey(Entity<EncryptionKeyHolderComponent> ent, IEquipmentLoadout? startingGear)
    {
        if (startingGear is not { } ||
            !startingGear.Equipment.TryGetValue(Slot, out var headsetId) ||
            !ProtoMan.Resolve(headsetId, out var proto) ||
            !proto.TryComp<ContainerFillComponent>(_fillName, out var fill) ||
            !fill.Containers.TryGetValue(EncryptionKeyHolderComponent.KeyContainerName, out var keys))
            return;

        _container.CleanContainer(ent.Comp.KeyContainer);
        foreach (var key in keys)
        {
            SpawnInContainerOrDrop(key, ent.Owner, ent.Comp.KeyContainer.ID);
        }
    }
}

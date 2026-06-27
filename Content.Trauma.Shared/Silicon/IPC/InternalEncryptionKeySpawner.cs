// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers;
using Content.Shared.Radio.Components;
using Content.Shared.Roles;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Silicon.IPC;

public sealed partial class InternalEncryptionKeySpawner : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EncryptionKeyHolderComponent, StartingGearEquippedEvent>(OnRoleAdded);
    }

    public void OnRoleAdded(Entity<EncryptionKeyHolderComponent> ent, ref StartingGearEquippedEvent ev)
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
        if (startingGear is not { }
            || !startingGear.Equipment.TryGetValue("ears", out var headsetId)
            || string.IsNullOrEmpty(headsetId))
            return;

        var headset = Spawn(headsetId, Transform(ent.Owner).Coordinates);
        if (!HasComp<EncryptionKeyHolderComponent>(headset)
            || !TryComp<ContainerFillComponent>(headset, out var fillComp)
            || !fillComp.Containers.TryGetValue(EncryptionKeyHolderComponent.KeyContainerName, out var defaultKeys))
            return;

        _container.CleanContainer(ent.Comp.KeyContainer);

        foreach (var key in defaultKeys)
        {
            SpawnInContainerOrDrop(key, ent.Owner, ent.Comp.KeyContainer.ID);
        }

        Del(headset);
    }
}

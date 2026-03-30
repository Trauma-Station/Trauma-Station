// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers;
using Content.Shared.Radio.Components;
using Content.Shared.Roles;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Silicon.IPC;

public sealed class InternalEncryptionKeySpawner : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StartingGearEquippedEvent>(OnRoleAdded);
    }

    public void OnRoleAdded(StartingGearEquippedEvent ev)
    {
        TryInsertEncryptionKey(ev.Entity, ev.StartingGear);
    }

    /// <summary>
    /// Inserts an IPC's encryption key from starting gear headset.
    /// </summary>
    /// <remarks>
    /// Doesn't support a profile's loadouts, have fun.
    /// </remarks>
    public void TryInsertEncryptionKey(EntityUid target, IEquipmentLoadout startingGear)
    {
        if (!TryComp<EncryptionKeyHolderComponent>(target, out var keyHolder)
            || !startingGear.Equipment.TryGetValue("ears", out var headsetId)
            || string.IsNullOrEmpty(headsetId))
            return;

        var headset = Spawn(headsetId, Transform(target).Coordinates);
        if (!HasComp<EncryptionKeyHolderComponent>(headset)
            || !TryComp<ContainerFillComponent>(headset, out var fillComp)
            || !fillComp.Containers.TryGetValue(EncryptionKeyHolderComponent.KeyContainerName, out var defaultKeys))
            return;

        _container.CleanContainer(keyHolder.KeyContainer);

        foreach (var key in defaultKeys)
        {
            SpawnInContainerOrDrop(key, target, keyHolder.KeyContainer.ID);
        }

        Del(headset);
    }
}

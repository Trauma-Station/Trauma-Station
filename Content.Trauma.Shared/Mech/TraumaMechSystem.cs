// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Emag.Systems;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Trauma.Common.Mech;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Mech;

public sealed partial class TraumaMechSystem : EntitySystem
{
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private SharedMechSystem _mech = default!;
    [Dependency] private EntityQuery<MechComponent> _mechQuery = default!;
    [Dependency] private EntityQuery<MechEquipmentComponent> _equipmentQuery = default!;

    [SubscribeLocalEvent]
    private void OnShotAttempted(Entity<MechEquipmentComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ent.Comp.EquipmentOwner is not {} mech || !_mechQuery.HasComp(mech))
        {
            args.Cancel();
            return;
        }

        // TODO: this should not be in an attempt event...
        var ev = new MechGunFiredEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    [SubscribeLocalEvent]
    private void OnEntGotRemovedFromContainer(Entity<MechPilotComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        // Fixes scram implants or teleports locking the pilot out of being able to move.
        _mech.TryEject(ent.Comp.Mech, pilot: ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnEmagged(Entity<MechComponent> ent, ref GotEmaggedEvent args)
    {
        if (!ent.Comp.BreakOnEmag ||
            ent.Comp.EquipmentWhitelist == null ||
            !_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        args.Handled = true;
        ent.Comp.EquipmentWhitelist = null;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnInsertAttempt(Entity<MechComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container != ent.Comp.EquipmentContainer ||
            _equipmentQuery.HasComp(args.EntityUid))
            return;

        // only equipment can go into the equipment container not bullet casings etc
        args.Cancel();
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body.Systems;
using Content.Shared.Forensics.Components;
using Content.Shared.Forensics.Systems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Trauma.Common.Forensics;

namespace Content.Trauma.Shared.Forensics;

public sealed partial class ScentSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private ForensicsSystem _forensics = default!;

    [SubscribeLocalEvent(after: [typeof(BloodstreamSystem)])]
    private void OnMapInit(Entity<ScentComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Scent = _forensics.GenerateFingerprint(length: 5);
        Dirty(ent);

        var forensics = EnsureComp<ForensicsComponent>(ent);
        forensics.Scent = ent.Comp.Scent;
        Dirty(ent, forensics);
    }

    [SubscribeLocalEvent]
    private void OnEquip(Entity<ScentComponent> ent, ref DidEquipEvent args)
    {
        ApplyScent(ent, args.Equipment);
    }

    [SubscribeLocalEvent]
    private void OnCleanupEvidence(Entity<ScentComponent> ent, ref ForensicsCleanedEvent args)
    {
        if (!TryComp<ForensicsComponent>(ent, out var targetComp))
            return;

        var generatedscent = _forensics.GenerateFingerprint(length: 5);
        ent.Comp.Scent = generatedscent;
        targetComp.Scent = generatedscent;
        Dirty(ent.Owner, targetComp);

        if (!_inventory.TryGetSlots(ent, out var slotDefinitions))
            return;

        foreach (var slot in slotDefinitions)
        {
            if (!_inventory.TryGetSlotEntity(ent, slot.Name, out var slotEnt))
                continue;

            var recipientComp = EnsureComp<ForensicsComponent>(slotEnt.Value);
            recipientComp.Scent = generatedscent;

            Dirty(slotEnt.Value, recipientComp);
        }
    }

    [SubscribeLocalEvent]
    private void OnScentCleanup(Entity<ScentComponent> ent, ref BeforeCleanEvent args)
    {
        args.CleanDelay += TimeSpan.FromSeconds(30);
    }

    private void ApplyScent(EntityUid user, EntityUid target)
    {
        if (HasComp<ScentComponent>(target))
            return;

        var component = EnsureComp<ForensicsComponent>(target);
        if (TryComp<ScentComponent>(user, out var scent))
            component.Scent = scent.Scent;

        Dirty(target, component);
    }
}

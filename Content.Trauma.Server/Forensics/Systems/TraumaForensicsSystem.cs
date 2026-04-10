// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Body.Systems;
using Content.Server.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Trauma.Common.Forensics;
using Content.Trauma.Server.Forensics.Components;

namespace Content.Trauma.Server.Forensics.Systems;
public sealed partial class TraumaForensicsSystem : EntitySystem
{
    [Dependency] private readonly ForensicsSystem _forensics = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScentComponent, DidEquipEvent>(OnEquip); // Einstein Engines
        SubscribeLocalEvent<ScentComponent, MapInitEvent>(OnScentInit, after: new[] { typeof(BloodstreamSystem) }); // Einstein Engines
        SubscribeLocalEvent<ScentComponent, ForensicsCleanedEvent>(OnCleanupEvidence);
        SubscribeLocalEvent<ScentComponent, BeforeCleanEvent>(OnScentCleanup);
    }

    private void OnEquip(EntityUid uid, ScentComponent component, DidEquipEvent args) // Einstein Engines
    {
        ApplyScent(uid, args.Equipment);
    }

    private void OnScentInit(EntityUid uid, ScentComponent component, MapInitEvent args) // Einstein Engines
    {
        component.Scent = _forensics.GenerateFingerprint(length: 5);

        var updatecomp = EnsureComp<ForensicsComponent>(uid);
        updatecomp.Scent = component.Scent;

        Dirty(uid, updatecomp);
    }

    private void OnCleanupEvidence(Entity<ScentComponent> ent, ref ForensicsCleanedEvent args)
    {
        if (!TryComp<ForensicsComponent>(ent, out var targetComp))
            return;

        var generatedscent = _forensics.GenerateFingerprint(length: 5);
        ent.Comp.Scent = generatedscent;
        targetComp.Scent = generatedscent;

        if (_inventory.TryGetSlots(ent, out var slotDefinitions))
            foreach (var slot in slotDefinitions)
            {
                if (!_inventory.TryGetSlotEntity(ent, slot.Name, out var slotEnt))
                    continue;

                EnsureComp<ForensicsComponent>(slotEnt.Value, out var recipientComp);
                recipientComp.Scent = generatedscent;

                Dirty(slotEnt.Value, recipientComp);
            }

        Dirty(ent.Owner, targetComp); // Einstein Engines - End
    }

    private void OnScentCleanup(Entity<ScentComponent> ent, ref BeforeCleanEvent args)
    {
        args.CleanDelay += 30;
    }

    private void ApplyScent(EntityUid user, EntityUid target) // Einstein Engines
    {
        if (HasComp<ScentComponent>(target))
            return;

        var component = EnsureComp<ForensicsComponent>(target);
        if (TryComp<ScentComponent>(user, out var scent))
            component.Scent = scent.Scent;

        Dirty(target, component);
    }
}

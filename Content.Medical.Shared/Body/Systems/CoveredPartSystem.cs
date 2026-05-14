// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Armor;
using Content.Shared.Body;
using Content.Shared.Explosion;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Medical.Shared.Body;

public sealed partial class CoveredPartSystem : EntitySystem
{
    [Dependency] private BodyPartSystem _part = default!;
    [Dependency] private EntityQuery<ArmorComponent> _armorQuery = default!;
    [Dependency] private EntityQuery<CoveredPartComponent> _query = default!;
    [Dependency] private EntityQuery<InventoryComponent> _inventoryQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, DidEquipEvent>(OnBodyEquip);
        SubscribeLocalEvent<BodyComponent, DidUnequipEvent>(OnBodyUnequip);

        SubscribeLocalEvent<CoveredPartComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<CoveredPartComponent, OrganGotRemovedEvent>(OnOrganRemoved);
        SubscribeLocalEvent<CoveredPartComponent, GetExplosionResistanceEvent>(OnGetExplosionResistance);
    }

    private void OnBodyEquip(Entity<BodyComponent> ent, ref DidEquipEvent args)
    {
        var item = args.Equipment;
        if (!_armorQuery.TryComp(item, out var armor))
            return;

        foreach (var partType in armor.ArmorCoverage)
        {
            foreach (var part in _part.GetBodyParts(ent, partType))
            {
                if (!_query.TryComp(part, out var covered))
                    continue;

                covered.Covered.Add(item);
                Dirty(part, covered);
            }
        }
    }

    private void OnBodyUnequip(Entity<BodyComponent> ent, ref DidUnequipEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return; // don't care about part coverage if the body is being deleted...

        var item = args.Equipment;
        if (!_armorQuery.TryComp(item, out var armor))
            return;

        foreach (var partType in armor.ArmorCoverage)
        {
            foreach (var part in _part.GetBodyParts(ent, partType))
            {
                if (!_query.TryComp(part, out var covered))
                    continue;

                covered.Covered.Remove(item);
                Dirty(part, covered);
            }
        }
    }

    private void OnOrganInserted(Entity<CoveredPartComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!_inventoryQuery.TryComp(args.Target, out var inventory) ||
            _part.GetPartType(ent) is not {} partType)
            return;

        ent.Comp.Covered.Clear(); // just incase..?

        // update coverage retroactively when (re)attaching parts
        var flags = SlotFlags.WITHOUT_POCKET;
        var slots = new InventorySystem.InventorySlotEnumerator(inventory, flags);
        while (slots.NextItem(out var item))
        {
            if (!_armorQuery.TryComp(item, out var armor) || !armor.ArmorCoverage.Contains(partType))
                continue;

            ent.Comp.Covered.Add(item);
        }

        if (ent.Comp.Covered.Count > 0)
            Dirty(ent);
    }

    private void OnOrganRemoved(Entity<CoveredPartComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (ent.Comp.Covered.Count == 0 || TerminatingOrDeleted(ent))
            return;

        // currently severed limbs can't have any clothes on them, so clear it just incase
        ent.Comp.Covered.Clear();
        Dirty(ent);
    }

    private void OnGetExplosionResistance(Entity<CoveredPartComponent> ent, ref GetExplosionResistanceEvent args)
    {
        foreach (var armor in ent.Comp.Covered)
        {
            RaiseLocalEvent(armor, ref args);
        }
    }
}

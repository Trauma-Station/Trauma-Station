// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Common.Clothing;
using Content.Shared.Body;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;

namespace Content.Medical.Shared.Body;

public sealed class BodyEquipmentSystem : EntitySystem
{
    [Dependency] private readonly BodyPartSystem _part = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<InventoryComponent> _inventoryQuery;

    // TODO SHITMED: make this a field on OrganCategoryPrototype or something??
    public static readonly Dictionary<BodyPartType, string[]> PartInventorySlots = new()
    {
        { BodyPartType.Head, [ "eyes", "ears", "head", "mask" ] },
        { BodyPartType.Torso, [ "innerclothing", "outerclothing" ] },
        { BodyPartType.Hand, [ "gloves" ] },
        { BodyPartType.Foot, [ "shoes" ] }
    };

    public override void Initialize()
    {
        base.Initialize();

        _inventoryQuery = GetEntityQuery<InventoryComponent>();

        SubscribeLocalEvent<BodyEquipmentComponent, IsEquippingTargetAttemptEvent>(OnEquippingTargetAttempt);
        SubscribeLocalEvent<BodyEquipmentComponent, CheckEquipmentPartEvent>(OnCheckEquipmentPart);
        SubscribeLocalEvent<BodyEquipmentComponent, OrganRemovedFromEvent>(OnOrganRemovedFrom);
    }

    private void OnEquippingTargetAttempt(Entity<BodyEquipmentComponent> ent, ref IsEquippingTargetAttemptEvent args)
    {
        if (GetInventoryPart(args.Slot) is not {} part ||
            HasBodyPart(ent.Owner, part))
            return;

        var ident = Identity.Entity(ent, EntityManager);
        var partName = part.ToString().ToLower();
        _popup.PopupClient(Loc.GetString("equip-part-missing-error",
            ("target", ident), ("part", partName)), args.Equipee, args.Equipee);
        args.Cancel();
    }

    private void OnCheckEquipmentPart(Entity<BodyEquipmentComponent> ent, ref CheckEquipmentPartEvent args)
    {
        // If there's no body part associated with the slot, just allow it
        if (GetInventoryPart(args.Slot) is not {} part)
        {
            args.Handled = true;
            return;
        }

        // only allow if this body has the required part
        if (HasBodyPart(ent, part))
            args.Handled = true;
    }

    private void OnOrganRemovedFrom(Entity<BodyEquipmentComponent> ent, ref OrganRemovedFromEvent args)
    {
        DropPartItems(ent.Owner, args.Organ.Owner);
    }

    private bool HasBodyPart(EntityUid body, BodyPartType part)
        => _part.FindBodyPart(body, part) != null;

    public static BodyPartType? GetInventoryPart(string slot)
        => slot switch
        {
            // TODO SHITMED: HOLY SHITCODE
            "innerclothing" or "outerclothing" => BodyPartType.Torso,
            "eyes" or "ears" or "head" or "mask" => BodyPartType.Head,
            "gloves" => BodyPartType.Hand,
            "shoes" => BodyPartType.Foot,
            _ => null
        };

    /// <summary>
    /// Drop items worn by a body part, unless there is another of the same type of part.
    /// </summary>
    public void DropPartItems(Entity<InventoryComponent?> ent, Entity<BodyPartComponent?> part)
    {
        // don't drop for mobs being deleted, gibbing etc would handle it themselves
        if (!TerminatingOrDeleted(ent) &&
            _inventoryQuery.Resolve(ent, ref ent.Comp) &&
            // only care about parts being removed
            _part.GetPartType(part) is {} type &&
            // do nothing if there's e.g. a second foot to wear shoes with. there's no individual shoe items.
            _part.GetBodyParts(ent.Owner, type).Count == 0 &&
            PartInventorySlots.TryGetValue(type, out var slots))
        {
            foreach (var slot in slots)
            {
                _inventory.DropSlotContents(ent, slot, ent.Comp);
            }
        }
    }
}

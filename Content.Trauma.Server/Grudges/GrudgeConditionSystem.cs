using Content.Goobstation.Shared.Contraband;
using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Objectives.Components;
using Content.Shared.Storage;
using Content.Trauma.Common.Grudge;
using Content.Trauma.Server.Grudges.Components;

namespace Content.Trauma.Server.Grudges;

public sealed partial class GrudgeConditionSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrudgeItemConditionComponent, ObjectiveGetProgressEvent>(OnGetItemProgress);

        SubscribeLocalEvent<GrudgeConditionComponent, GrudgeAddedEvent>(OnStartupGrudge);
        SubscribeLocalEvent<GrudgeItemConditionComponent, GrudgeAddedEvent>(OnStartupItemGrudge);

        SubscribeLocalEvent<GrudgeItemComponent, ExaminedEvent>(OnExaminedItemGrudge);
    }

    private void OnGetItemProgress(Entity<GrudgeItemConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (!TryComp<GrudgeConditionComponent>(ent, out var comp))
        {
            Log.Error($"{Name(ent)} is not a grudgee objective. Remove Grudge Item Condition.");
            return;
        }

        args.Progress = GetItemProgress(ent.Comp.Item, args.Mind.CurrentEntity, comp.Grudge);
    }

    private void OnStartupGrudge(Entity<GrudgeConditionComponent> ent, ref GrudgeAddedEvent args)
    {
        // if accuser, target is accused, and vice versa.
        if (ent.Owner == args.AccuserObjective)
            ent.Comp.Grudge = args.Accused;
        else if (ent.Owner == args.AccusedObjective)
            ent.Comp.Grudge = args.Accuser;
    }

    private void OnStartupItemGrudge(Entity<GrudgeItemConditionComponent> ent, ref GrudgeAddedEvent args)
    {
        // Make sure accuser logic runs first
        if (ent.Owner == args.AccusedObjective)
        {
            if (TryComp<GrudgeItemConditionComponent>(args.AccuserObjective, out var comp))
            {
                ent.Comp.Item = comp.Item;
                ent.Comp.ItemId = comp.ItemId;
            }
            return;
        }

        // Item spawning.
        var item = Spawn(ent.Comp.ItemId, args.Accused.ToCoordinates());
        ent.Comp.Item = item;
        var grudge = EnsureComp<GrudgeItemComponent>(item);
        grudge.Grudgee = args.Accuser;
        _hands.PickupOrDrop(args.Accused, item);
    }

    private void OnExaminedItemGrudge(Entity<GrudgeItemComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Grudgee != args.Examiner)
            return;

        args.PushMarkup("This is your item!");
    }

    private float GetItemProgress(EntityUid? targetItem, EntityUid? fakeSelf, EntityUid? otherGuy)
    {
        if (targetItem is not { } item)
            return 0.0f; // Item's gone, you failed yourself

        if (fakeSelf is not { } self)
            return 0.0f; // Not real?

        if (otherGuy is { } guy && FindItem(guy, item))
            return 0.0f; // Other guy has it.

        if (FindItem(self, item))
            return 1.0f; // You got it.

        return 0.5f; // It's fucking somewhere, go find it.
    }


    public bool FindItem(EntityUid uid, EntityUid grudgeItem)
    {
        List<EntityUid> listOfItems = new();
        List<EntityUid> itemsToCheck = new();

        if (uid == grudgeItem)
            return true;

        // Check items in inner storage
        if (RecursiveFindInStorage(uid, grudgeItem))
            return true;

        // Check items in inventory slots and storages
        var enumerator = _inventory.GetSlotEnumerator(uid);
        while (enumerator.MoveNext(out var slot))
        {
            var item = slot.ContainedEntity;

            if (item == null)
                continue;

            if (item == grudgeItem)
                return true;

            if (RecursiveFindInStorage(item.Value, grudgeItem))
                return true;
        }

        // Check items in hands
        var handEnumerator = _hands.EnumerateHeld(uid);
        foreach (var handItem in handEnumerator)
        {
            if (handItem == grudgeItem)
                return true;

            if (RecursiveFindInStorage(handItem, grudgeItem))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check items with storage component (like bags) to prevent check in itemSlots, implants.
    /// </summary>
    /// <param name="uid"></param>
    /// <returns></returns>
    private bool RecursiveFindInStorage(EntityUid uid, EntityUid grudgeItem, HashSet<EntityUid>? visited = null)
    {
        visited ??= new HashSet<EntityUid>();
        List<EntityUid> listToCheck = new();

        // Prevents rechecking same entity
        if (!visited.Add(uid))
            return false;

        if (!TryComp<StorageComponent>(uid, out var storage) || storage.Container.ContainedEntities.Count == 0)
            return false;

        foreach (var item in storage.Container.ContainedEntities)
        {
            if (item == grudgeItem)
                return true;
            listToCheck.Add(item);

            if (RecursiveFindInStorage(item, grudgeItem, visited))
                return true;
        }

        return false;
    }
}

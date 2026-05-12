// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Storage;
using Content.Trauma.Common.Grudge;
using Content.Trauma.Server.Grudges.Components;

namespace Content.Trauma.Server.Grudges;

public sealed partial class GrudgeConditionSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrudgeItemConditionComponent, ObjectiveGetProgressEvent>(OnGetItemProgress);

        SubscribeLocalEvent<GrudgeConditionComponent, GrudgeAddedEvent>(OnStartupGrudge);
        SubscribeLocalEvent<GrudgeConditionComponent, GrudgeSetupEvent>(OnSetupGrudge);
        SubscribeLocalEvent<GrudgeItemConditionComponent, GrudgeAddedEvent>(OnStartupItemGrudge);
        SubscribeLocalEvent<GrudgeItemConditionComponent, GrudgeSetupEvent>(OnSetupItemGrudge);
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
        {
            ent.Comp.Guy = args.Accused;
            ent.Comp.Grudge = args.AccusedObjective;
            ent.Comp.IsAccuser = true;
        }
        else if (ent.Owner == args.AccusedObjective)
        {
            ent.Comp.Guy = args.Accuser;
            ent.Comp.Grudge = args.AccuserObjective;
            ent.Comp.IsAccuser = false;
        }
    }

    private void OnSetupGrudge(Entity<GrudgeConditionComponent> ent, ref GrudgeSetupEvent args)
    {
        if (ent.Comp.Guy is not { } guy)
            return;

        var description = $"The guy is {Name(guy)}";
        _meta.SetEntityDescription(ent.Owner, $"{Description(ent.Owner)}\n{description}", MetaData(ent.Owner));
    }

    private void OnStartupItemGrudge(Entity<GrudgeItemConditionComponent> ent, ref GrudgeAddedEvent args)
    {
        // Make sure accuser logic runs first
        if (ent.Owner == args.AccusedObjective)
        {
            if (!TryComp<GrudgeItemConditionComponent>(args.AccuserObjective, out var comp))
                return;

            ent.Comp.Item = comp.Item;
            ent.Comp.ItemId = comp.ItemId;
            return;
        }

        // Item spawning.
        var newItem = Spawn(ent.Comp.ItemId, args.Accused.ToCoordinates());
        ent.Comp.Item = newItem;
        var grudge = EnsureComp<GrudgeItemComponent>(newItem);
        grudge.Grudgee = args.Accuser;
        _hands.PickupOrDrop(args.Accused, newItem);
    }

    private void OnSetupItemGrudge(Entity<GrudgeItemConditionComponent> ent, ref GrudgeSetupEvent args)
    {
        if (!TryComp<GrudgeConditionComponent>(ent.Owner, out var comp))
            return;

        if (ent.Comp.Item is not { } item)
            return;

        string description = "";

        if (comp.IsAccuser)
            description = $"He has your {Name(item)}!";
        else
            description = $"You have his {Name(item)}!";

        _meta.SetEntityDescription(ent.Owner, $"{Description(ent.Owner)}\n{description}", MetaData(ent.Owner));
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

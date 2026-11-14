// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat.RadioIconsEvents;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Stunnable;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Common.Speech;
using Content.Trauma.Common.Strip;
using Content.Trauma.Common.Weapons;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Tackle;
using Content.Trauma.Shared.Viewcone;
using Content.Trauma.Shared.Waypointer.Events;
using Content.Trauma.Shared.Weapons.SheathCounterattack;
using Content.Trauma.Common.Projectiles;
using Content.Trauma.Shared.Xenomorphs;
using Content.Shared.Clothing.Components;
using Content.Trauma.Common.Inventory;
using Content.Shared.Clothing.EntitySystems;

namespace Content.Trauma.Shared.Inventory;

public sealed partial class TraumaInventorySystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private ToggleableClothingSystem _toggleable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, ThievingStealthCheckEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, TackleEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, GetCounterAttackSheathEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, CalculateTackleModifierEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, CheckMagicItemEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, TransformSpeakerJobIconEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, BeforeHarmfulActionEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, CanSeeOnCameraEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, ModifyViewconeAngleEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, GetStandUpTimeEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, BeforeMovespeedModifierAppliedEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, WaypointerChangedEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, CanSpawnFootstepsEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, BeforeDamageChangedEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, SpeechFontOverrideEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, GotHitByProjectileEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, FaceHuggerEquipAttemptEvent>(_inventory.RelayEvent);
    }

    [SubscribeLocalEvent]
    private void OnAttachedTransfer(Entity<AttachedClothingComponent> ent, ref InventoryItemTransferEvent args)
    {
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnToggleableTransfer(Entity<ToggleableClothingComponent> ent, ref InventoryItemTransferEvent args)
    {
        if (ent.Comp.ClothingUid is not { } uid || ent.Comp.Container is not { } container ||
            container.Contains(uid)) // If it's not toggled - transfer normally
            return;

        args.Handled = true;
        _inventory.TryUnequip(args.Source, args.Slot, true, true, triggerHandContact: true);
        if (!_inventory.TryEquip(args.Target, ent, args.Slot, true, true, triggerHandContact: true))
            return;

        // Otherwise toggle after transfer
        _toggleable.ToggleClothing(args.Target, ent, ent.Comp);
    }
}

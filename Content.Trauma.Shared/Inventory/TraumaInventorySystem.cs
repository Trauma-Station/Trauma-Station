// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat.RadioIconsEvents;
using Content.Shared.Inventory;
using Content.Trauma.Common.Glue;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Common.Lube;
using Content.Trauma.Common.Weapons;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Tackle;
using Content.Trauma.Shared.Viewcone;

namespace Content.Trauma.Shared.Inventory;

public sealed class TraumaInventorySystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, TackleEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, CalculateTackleModifierEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, CheckMagicItemEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, TransformSpeakerJobIconEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, BeforeHarmfulActionEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, CanSeeOnCameraEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, GluedPickUpAttemptEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, LubedPickUpAttemptEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, ModifyViewconeAngleEvent>(_inventory.RelayEvent);
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Ghost.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Blocking;

namespace Content.Trauma.Shared.BloodCult.Items;

public sealed partial class CultItemSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private EntityQuery<GhostComponent> _ghostQuery = default!;

    [SubscribeLocalEvent]
    private void OnActivate(Entity<CultItemComponent> item, ref ActivateInWorldEvent args)
    {
        if (CanUse(args.User))
            return;

        args.Handled = true;
        KnockdownAndDropItem(item, args.User, "cult-item-component-generic");
    }

    [SubscribeLocalEvent]
    private void OnBeforeThrow(Entity<CultItemComponent> item, ref BeforeThrowEvent args)
    {
        if (CanUse(args.PlayerUid))
            return;

        args.Cancelled = true;
        KnockdownAndDropItem(item, args.PlayerUid, "cult-item-component-throw-fail");
    }

    [SubscribeLocalEvent]
    private void OnEquipAttempt(Entity<CultItemComponent> item, ref BeingEquippedAttemptEvent args)
    {
        if (CanUse(args.EquipTarget))
            return;

        args.Cancel();
        KnockdownAndDropItem(item, args.EquipTarget, "cult-item-component-equip-fail");
    }

    [SubscribeLocalEvent]
    private void OnMeleeAttempt(Entity<CultItemComponent> item, ref AttemptMeleeEvent args)
    {
        if (CanUse(args.User))
            return;

        args.Cancelled = true;
        KnockdownAndDropItem(item, args.User, "cult-item-component-attack-fail");
    }

    [SubscribeLocalEvent]
    private void OnBlockAttempt(Entity<CultItemComponent> item, ref BlockAttemptEvent args)
    {
        if (CanUse(args.User))
            return;

        args.Cancelled = true;
        KnockdownAndDropItem(item, args.User, "cult-item-component-block-fail");
    }

    private void KnockdownAndDropItem(Entity<CultItemComponent> item, EntityUid user, LocId message)
    {
        _popup.PopupEntity(Loc.GetString(message), item, user);
        _stun.TryKnockdown(user, item.Comp.KnockdownDuration, true);
        _hands.TryDrop(user);
    }

    private bool CanUse(EntityUid uid)
        => _ghostQuery.HasComp(uid) || _cult.IsCultist(uid);
}

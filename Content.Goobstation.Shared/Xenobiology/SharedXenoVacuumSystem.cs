// SPDX-FileCopyrightText: 2025 August Eymann <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 TheBorzoiMustConsume <197824988+TheBorzoiMustConsume@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Goobstation.Shared.Xenobiology.Components.Equipment;
using Content.Shared.Coordinates;
using Content.Shared.Destructible;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Goobstation.Shared.Xenobiology;

/// <summary>
/// This handles all interactions with xenovac.
/// AI toggling is done in server.
/// </summary>
public abstract class SharedXenoVacuumSystem : EntitySystem
{
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    private const string ReleaseDelayId = "release";
    private const string SuctionDelayId = "suction";

    private EntityQuery<EmaggedComponent> _emaggedQuery;
    private EntityQuery<MobStateComponent> _mobQuery;
    private EntityQuery<UseDelayComponent> _delayQuery;
    private EntityQuery<XenoVacuumTankComponent> _tankQuery;

    public override void Initialize()
    {
        base.Initialize();

        _emaggedQuery = GetEntityQuery<EmaggedComponent>();
        _mobQuery = GetEntityQuery<MobStateComponent>();
        _delayQuery = GetEntityQuery<UseDelayComponent>();
        _tankQuery = GetEntityQuery<XenoVacuumTankComponent>();

        SubscribeLocalEvent<XenoVacuumTankComponent, ComponentInit>(OnTankInit);
        SubscribeLocalEvent<XenoVacuumTankComponent, ExaminedEvent>(OnTankExamined);
        SubscribeLocalEvent<XenoVacuumTankComponent, DestructionEventArgs>(OnDestruction);

        SubscribeLocalEvent<XenoVacuumComponent, GotEmaggedEvent>(OnGotEmagged);
        SubscribeLocalEvent<XenoVacuumComponent, GotEquippedHandEvent>(OnEquippedHand);
        SubscribeLocalEvent<XenoVacuumComponent, GotUnequippedHandEvent>(OnUnequippedHand);
        SubscribeLocalEvent<XenoVacuumComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnTankInit(Entity<XenoVacuumTankComponent> ent, ref ComponentInit args)
    {
        ent.Comp.StorageTank = _container.EnsureContainer<Container>(ent, ent.Comp.TankContainerName);
    }

    private void OnTankExamined(Entity<XenoVacuumTankComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var text = Loc.GetString("xeno-vacuum-examined", ("n", ent.Comp.StorageTank.ContainedEntities.Count));
        args.PushMarkup(text);
    }

    private void OnDestruction(Entity<XenoVacuumTankComponent> ent, ref DestructionEventArgs args)
    {
        // apparently ContainerManager doesn't automatically release them so
        _container.EmptyContainer(ent.Comp.StorageTank);
    }

    private void OnEquippedHand(Entity<XenoVacuumComponent> ent, ref GotEquippedHandEvent args)
    {
        SetTankNozzle(args.User, ent);
        if (GetTank(args.User) is not {} tank)
            return;

        tank.Comp.LinkedNozzle = ent;
        Dirty(tank);
    }

    private void OnUnequippedHand(Entity<XenoVacuumComponent> ent, ref GotUnequippedHandEvent args)
    {
        SetTankNozzle(args.User, null);
    }

    private void OnGotEmagged(Entity<XenoVacuumComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction) ||
            _emag.CheckFlag(ent, EmagType.Interaction) ||
            _emaggedQuery.HasComp(ent))
            return;

        args.Handled = true;
    }

    private void OnAfterInteract(Entity<XenoVacuumComponent> ent, ref AfterInteractEvent args)
    {
        var delay = _delayQuery.Comp(ent);
        if (CheckDelays((ent, delay))) return;

        if (args.CanReach && args.Target is {} target && _mobQuery.HasComp(target))
        {
            TryDoSuction(args.User, target, ent);
            _useDelay.TryResetDelay((ent, delay), false, SuctionDelayId);
            return;
        }

        if (GetTank(args.User) is not {} tank || tank.Comp.StorageTank.ContainedEntities.Count <= 0)
            return;

        foreach (var removedEnt in _container.EmptyContainer(tank.Comp.StorageTank))
        {
            var identity = Identity.Entity(removedEnt, EntityManager);
            var popup = Loc.GetString("xeno-vacuum-clear-popup", ("ent", identity));
            _popup.PopupClient(popup, ent, args.User);

            var coords = args.Target?.ToCoordinates() ?? args.ClickLocation;
            _throw.TryThrow(removedEnt, coords);

            _stun.TryUpdateParalyzeDuration(removedEnt, TimeSpan.FromSeconds(2));
            SetHTNEnabled(removedEnt, true, 2f);
        }

        _useDelay.TryResetDelay((ent, delay), false, ReleaseDelayId);

        _audio.PlayEntity(ent.Comp.ClearSound, ent, args.User, AudioParams.Default.WithVolume(-2f));
    }

    #region Helpers

    private bool CheckDelays(Entity<UseDelayComponent?> ent)
        => _useDelay.IsDelayed(ent, SuctionDelayId)
        || _useDelay.IsDelayed(ent, ReleaseDelayId);

    private Entity<XenoVacuumTankComponent>? GetTank(EntityUid user)
    {
        foreach (var item in _hands.EnumerateHeld(user))
        {
            if (_tankQuery.TryComp(item, out var comp))
                return (item, comp);
        }

        if (!_inventory.TryGetContainerSlotEnumerator(user, out var slotEnum, SlotFlags.WITHOUT_POCKET))
            return null;

        while (slotEnum.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is not {} item)
                continue;

            if (_tankQuery.TryComp(item, out var comp))
                return (item, comp);
        }

        return null;
    }

    private void SetTankNozzle(EntityUid user, EntityUid? nozzle)
    {
        if (GetTank(user) is not {} tank)
            return;

        tank.Comp.LinkedNozzle = nozzle;
        Dirty(tank);
    }

    private bool TryDoSuction(EntityUid user, EntityUid target, Entity<XenoVacuumComponent> vacuum)
    {
        if (GetTank(user) is not {} tank)
        {
            var noTankPopup = Loc.GetString("xeno-vacuum-suction-fail-no-tank-popup");
            _popup.PopupClient(noTankPopup, vacuum, user);
            return false;
        }

        var identity = Identity.Entity(target, EntityManager);
        if (!_emaggedQuery.HasComp(vacuum) && _whitelist.IsWhitelistFail(vacuum.Comp.EntityWhitelist, target))
        {
            var invalidEntityPopup = Loc.GetString("xeno-vacuum-suction-fail-invalid-entity-popup", ("ent", identity));
            _popup.PopupClient(invalidEntityPopup, vacuum, user);

            return false;
        }

        if (tank.Comp.StorageTank.ContainedEntities.Count > tank.Comp.MaxEntities)
        {
            var tankFullPopup = Loc.GetString("xeno-vacuum-suction-fail-tank-full-popup");
            _popup.PopupClient(tankFullPopup, vacuum, user);

            return false;
        }

        SetHTNEnabled(target, false);

        if (!_container.Insert(target, tank.Comp.StorageTank))
        {
            Log.Error($"{ToPrettyString(user)} failed to insert {ToPrettyString(target)} into {ToPrettyString(tank)}");
            return false;
        }

        _audio.PlayPredicted(vacuum.Comp.Sound, user, user);
        var successPopup = Loc.GetString("xeno-vacuum-suction-succeed-popup", ("ent", identity));
        _popup.PopupClient(successPopup, vacuum, user);

        return true;
    }

    protected virtual void SetHTNEnabled(EntityUid uid, bool enabled, float planCooldown = 0f)
    {
    }

    #endregion
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Clothing.Components;
using Content.Shared.Cuffs;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Trauma.Common.RadialSelector;
using Content.Trauma.Shared.BloodCult.Empower;
using System.Linq;

namespace Content.Trauma.Shared.BloodCult.Spells;

public sealed partial class BloodCultSpellsSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedCuffableSystem _cuffable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private StatusEffectsSystem _status = default!;

    private static readonly EntProtoId Muted = "StatusEffectMuted";

    #region Event Handlers

    [SubscribeLocalEvent]
    private void OnStartup(Entity<BloodCultSpellsComponent> ent, ref ComponentStartup args)
    {
        _ui.SetUi(ent.Owner, CultSpellsUiKey.Key, new InterfaceData("CultSpellsBUI", 0f, false));
    }

    [SubscribeLocalEvent]
    private void OnCultSpellAttempt(Entity<CultSpellComponent> ent, ref ActionAttemptEvent args)
    {
        var user = args.User;
        if (args.Cancelled || _blocker.CanSpeak(user))
            return;

        _popup.PopupEntity("You can't speak the incantation!", user, user, PopupType.MediumCaution);
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnCultSpellValidate(Entity<CultSpellComponent> ent, ref ActionValidateEvent args)
    {
        if (ent.Comp.BypassProtection || args.Invalid || args.Input.EntityTarget is not { } netTarget)
            return;

        var target = GetEntity(netTarget);

        // TODO: actual magic protection shit, show a popup
        if (HasComp<MindShieldComponent>(target))
        {
            var user = args.User;
            _popup.PopupEntity("Mind protection blocks your magic!", user, user, PopupType.MediumCaution);
            args.Invalid = true;
        }
    }

    [SubscribeLocalEvent]
    private void OnActionRemoved(Entity<BloodCultSpellsComponent> ent, ref ActionRemovedEvent args)
    {
        if (ent.Comp.ActiveSpells.Remove(args.Action))
            Dirty(ent);
    }

    private int GetLimit(EntityUid user)
    {
        var limit = 1;
        if (TryComp<BloodCultEmpoweredComponent>(user, out var empowered))
            limit += empowered.ExtraSpells;
        return limit;
    }

    [SubscribeLocalEvent]
    private void OnSpellSelected(Entity<BloodCultSpellsComponent> ent, ref CultSpellSelectedMessage args)
    {
        var user = args.Actor;
        var i = args.Index;
        if (i < 0 || i >= ent.Comp.AvailableActions.Count)
            return;

        var id = ent.Comp.AvailableActions[i];
        if (GetActiveSpell(ent, id) is { } action)
        {
            _popup.PopupEntity("You remove your current spell", user, user);
            _actions.RemoveAction(user, action);
            return;
        }

        var createSpellEvent = new CreateSpellDoAfterEvent(id);
        var doAfter = new DoAfterArgs(EntityManager,
            args.Actor,
            ent.Comp.SpellCreationTime,
            createSpellEvent,
            eventTarget: ent)
        {
            BreakOnMove = true
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    [SubscribeLocalEvent]
    private void OnSpellCreated(Entity<BloodCultSpellsComponent> ent, ref CreateSpellDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var user = args.User;
        var count = ent.Comp.ActiveSpells.Count;
        if (count >= GetLimit(args.User))
        {
            if (count != 1)
            {
                _popup.PopupEntity("You need to remove another spell first!", user, user, PopupType.MediumCaution);
                return;
            }

            // just swap the spell if unempowered, where 1 is the limit
            var old = ent.Comp.ActiveSpells.First();
            _actions.RemoveAction(user, old);
        }

        if (_actions.AddAction(user, args.ActionProtoId, container: ent) is not { } action)
            return;

        _popup.PopupEntity($"You prepare your {Name(action)} spell", user, user, PopupType.Medium);
        _actions.SetTemporary(action, true); // can't be temp in the prototype or AddAction will queue del it :D
        ent.Comp.ActiveSpells.Add(action);
        Dirty(ent);
    }

    #endregion

    #region SpellsHandlers

    [SubscribeLocalEvent]
    private void OnShackles(BloodCultShacklesEvent ev)
    {
        if (ev.Handled)
            return;

        var cuffs = PredictedSpawnAtPosition(ev.ShacklesProto, Transform(ev.Target).Coordinates);
        if (!_cuffable.TryAddNewCuffs(ev.Performer, ev.Target, cuffs))
        {
            PredictedDel(cuffs);
            return;
        }

        _stun.TryKnockdown(ev.Target, ev.KnockdownDuration, true);
        _status.TryUpdateStatusEffectDuration(ev.Target, Muted, ev.MuteDuration);
        ev.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnSummonEquipment(SummonEquipmentEvent ev)
    {
        if (ev.Handled)
            return;

        var coords = Transform(ev.Performer).Coordinates;
        foreach (var (slot, protoId) in ev.Prototypes)
        {
            var entity = PredictedSpawnAtPosition(protoId, coords);
            _hands.TryPickupAnyHand(ev.Performer, entity);
            _inventory.TryUnequip(ev.Performer, slot);
            _inventory.TryEquip(ev.Performer, entity, slot, force: true);
        }

        ev.Handled = true;
    }

    #endregion

    #region Helpers

    private EntityUid? GetActiveSpell(Entity<BloodCultSpellsComponent> ent, string id)
    {
        foreach (var action in ent.Comp.ActiveSpells)
        {
            if (id == Prototype(action)?.ID)
                return action;
        }

        return null;
    }

    #endregion
}

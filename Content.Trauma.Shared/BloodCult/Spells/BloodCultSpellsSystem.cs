// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Religion;
using Content.Medical.Common.Targeting;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Body.Components;
using Content.Shared.Cuffs;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Trauma.Shared.BloodCult.Empower;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Trauma.Shared.BloodCult.Spells;

public sealed partial class BloodCultSpellsSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedCuffableSystem _cuffable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private EntityQuery<BloodstreamComponent> _bloodQuery = default!;

    private static readonly EntProtoId Muted = "StatusEffectMuted";
    private static readonly ProtoId<DamageTypePrototype> Slash = "Slash";

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

        var ev = new BeforeCastTouchSpellEvent(target);
        RaiseLocalEvent(target, ref ev, true);
        args.Invalid = ev.Cancelled;
    }

    [SubscribeLocalEvent]
    private void OnActionRemoved(Entity<BloodCultSpellsComponent> ent, ref ActionRemovedEvent args)
    {
        if (ent.Comp.ActiveSpells.Remove(args.Action))
            Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnSpellSelected(Entity<BloodCultSpellsComponent> ent, ref CultSpellSelectedMessage args)
    {
        var user = args.Actor;
        var i = args.Index;
        if (i < 0 || i >= ent.Comp.AvailableActions.Count)
            return;

        // can't do blood magic in a robot or pai
        if (!_bloodQuery.HasComp(user))
        {
            _popup.PopupEntity("You have no blood to channel the spell through!", user, user, PopupType.LargeCaution);
            return;
        }

        var time = ent.Comp.SpellCreationTime;
        var limit = ent.Comp.SpellsLimit;
        if (TryComp<BloodCultEmpoweredComponent>(user, out var empowered))
        {
            time *= empowered.SpellCreationTimeMultiplier;
            limit += empowered.ExtraSpells;
        }

        var id = ent.Comp.AvailableActions[i];
        var count = ent.Comp.ActiveSpells.Count;
        if (count >= limit)
        {
            if (GetActiveSpell(ent, id) is { } action)
            {
                _popup.PopupEntity("You forget your current spell", user, user);
                _actions.RemoveAction(user, action);
                return;
            }

            _popup.PopupEntity("You need to remove another spell first!", user, user, PopupType.MediumCaution);
        }

        _popup.PopupEntity("You begin to carve unnatural symbols into your flesh!", user, user, PopupType.MediumCaution);

        var createSpellEvent = new CreateSpellDoAfterEvent(id);
        var doAfter = new DoAfterArgs(EntityManager,
            args.Actor,
            time,
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

        if (_actions.AddAction(user, args.ActionProtoId, container: ent) is not { } action)
            return;

        var damage = ent.Comp.SpellCreatingDamage;
        if (TryComp<BloodCultEmpoweredComponent>(user, out var empowered))
            damage *= empowered.SpellCreationDamageMultiplier;

        var damageSpec = new DamageSpecifier()
        {
            DamageDict = new()
            {
                { Slash, damage }
            },
        };

        var targetArm = ChooseTargetArm(user);
        _damage.ChangeDamage(user, damageSpec, targetPart: targetArm, canMiss: false);

        _popup.PopupEntity($"Your wounds glow with power, you have prepared a {Name(action)} invocation!", user, user, PopupType.Medium);
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
        if (!_cuffable.TryAddNewCuffs(ev.Target, ev.Performer, cuffs))
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


    public TargetBodyPart ChooseTargetArm(EntityUid uid)
    {
        var targetArm = TargetBodyPart.LeftArm;
        if (SharedRandomExtensions.PredictedProb(_timing, .5f, GetNetEntity(uid)))
            targetArm = TargetBodyPart.RightArm;

        return targetArm;
    }

    #endregion
}

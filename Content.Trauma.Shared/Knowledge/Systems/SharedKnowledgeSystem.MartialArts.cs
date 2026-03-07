// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.MartialArts;
using Content.Trauma.Shared.MartialArts.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Knowledge.Systems;

public abstract partial class SharedKnowledgeSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;

    private EntityQuery<MartialArtsKnowledgeComponent> _artQuery;

    private void InitializeMartialArts()
    {
        _artQuery = GetEntityQuery<MartialArtsKnowledgeComponent>();

        SubscribeLocalEvent<MartialArtsKnowledgeComponent, KnowledgeAddedEvent>(OnMartialArtAdded);
        SubscribeLocalEvent<MartialArtsKnowledgeComponent, KnowledgeRemovedEvent>(OnMartialArtRemoved);

        SubscribeLocalEvent<ComboActionsComponent, KnowledgeEnabledEvent>(OnComboActionsEnabled);
        SubscribeLocalEvent<ComboActionsComponent, KnowledgeDisabledEvent>(OnComboActionsDisabled);

        SubscribeLocalEvent<KnowledgeHolderComponent, ShotAttemptedEvent>(RelayMartialArt);
        SubscribeLocalEvent<NoGunComponent, ShotAttemptedEvent>(OnNoGunShotAttempted);
        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeInteractHandEvent>(OnInteract);
        SubscribeLocalEvent<KnowledgeHolderComponent, ComboAttackPerformedEvent>(RelayMartialArt);
        SubscribeLocalEvent<KnowledgeHolderComponent, MeleeHitEvent>(RelayActiveEvent);
        SubscribeLocalEvent<KnowledgeHolderComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<KnowledgeHolderComponent, CheckGrabOverridesEvent>(CheckGrabStageOverridePass);
        SubscribeLocalEvent<KnowledgeHolderComponent, RefreshMovementSpeedModifiersEvent>(OnSpeedModifier);
        SubscribeLocalEvent<KnowledgeHolderComponent, GetMeleeAttackRateEvent>(RelayActiveEvent);
        SubscribeLocalEvent<KnowledgeHolderComponent, ProjectileReflectAttemptEvent>(OnProjectileHit);
        SubscribeLocalEvent<PerformMartialArtComboEvent>(OnComboActionClicked);

        SubscribeAllEvent<KnowledgeUpdateMartialArtsEvent>(OnUpdateMartialArts);
    }

    private void OnMartialArtAdded(Entity<MartialArtsKnowledgeComponent> ent, ref KnowledgeAddedEvent args)
    {
        // if you learn a martial art without one active, automatically select it
        if (args.Container.Comp.ActiveMartialArt != null)
            return;

        ChangeMartialArts(args.Container, args.Holder, ent);
    }

    private void OnMartialArtRemoved(Entity<MartialArtsKnowledgeComponent> ent, ref KnowledgeRemovedEvent args)
    {
        if (args.Container.Comp.ActiveMartialArt == ent.Owner)
            ChangeMartialArts(args.Container, args.Holder, null); // disables the skill internally
    }

    private void OnComboActionsEnabled(Entity<ComboActionsComponent> ent, ref KnowledgeEnabledEvent args)
    {
        var user = args.Holder;
        foreach (var (comboId, actionId) in ent.Comp.StoredComboActions)
        {
            if (_actions.AddAction(user, actionId) is { } action)
                ent.Comp.ComboActions[comboId] = action;
        }
        Dirty(ent);
    }

    private void OnComboActionsDisabled(Entity<ComboActionsComponent> ent, ref KnowledgeDisabledEvent args)
    {
        var user = args.Holder;
        foreach (var action in ent.Comp.ComboActions.Values)
        {
            _actions.RemoveAction(user, action);
        }
        ent.Comp.ComboActions.Clear();
        Dirty(ent);
    }

    private void OnNoGunShotAttempted(Entity<NoGunComponent> ent, ref ShotAttemptedEvent args)
    {
        _popup.PopupClient(Loc.GetString("gun-disabled"), args.User, args.User);
        args.Cancel();
    }

    private void OnInteract(Entity<KnowledgeHolderComponent> ent, ref BeforeInteractHandEvent args)
    {
        if (ent.Owner == args.Target || !HasComp<MobStateComponent>(args.Target))
            return;

        if (GetActiveMartialArt(ent) is not { } skill)
            return;

        // TODO: give this a cooldown
        var ev = new ComboAttackPerformedEvent(ent.Owner, args.Target, ent.Owner, ComboAttackType.Hug);
        RaiseLocalEvent(skill, ref ev);
    }

    private void OnDamageChanged(Entity<KnowledgeHolderComponent> ent, ref DamageChangedEvent args)
    {
        // ignore healing
        if (args.DamageDelta is not { } delta || !args.DamageIncreased ||
            // ignore things like radiation
            args.Origin == null || !args.InterruptsDoAfters ||
            // pvs can remove the brain sometimes so dont get trolled
            _timing.ApplyingState || !_timing.IsFirstTimePredicted)
            return;

        var ev = new TookDamageEvent(ent, delta.GetTotal().Int());
        RelayActiveEvent(ent, ref ev);
    }

    private void OnUpdateMartialArts(KnowledgeUpdateMartialArtsEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player ||
            GetContainer(player) is not { } ent)
            return;

        var unit = ev.Knowledge is { } id
            ? GetKnowledge(ent, id)
            : null;

        if (unit != null && !_artQuery.HasComp(unit))
            return; // no setting construction as your martial art...

        ChangeMartialArts(ent, player, unit);
    }

    public void ChangeMartialArts(Entity<KnowledgeContainerComponent> ent, EntityUid user, EntityUid? knowledgeUid)
    {
        if (ent.Comp.ActiveMartialArt == knowledgeUid)
            return; // no change

        if (ent.Comp.ActiveMartialArt is { } old)
        {
            var ev = new KnowledgeDisabledEvent(ent, user);
            RaiseLocalEvent(old, ref ev);
        }

        ent.Comp.ActiveMartialArt = knowledgeUid;
        DirtyField(ent, ent.Comp, nameof(ent.Comp.ActiveMartialArt));

        if (knowledgeUid is { } unit)
        {
            DebugTools.Assert(_artQuery.HasComp(unit),
                $"Tried to use {ToPrettyString(knowledgeUid)} as martial art for {ToPrettyString(user)}!");
            var ev = new KnowledgeEnabledEvent(ent, user);
            RaiseLocalEvent(unit, ref ev);
            _popup.PopupClient(Loc.GetString("knowledge-martial-art-selected", ("name", Name(unit))), user, user);
        }
        else
        {
            _popup.PopupClient(Loc.GetString("knowledge-martial-art-deselected"), user, user);
        }
        _speed.RefreshMovementSpeedModifiers(user);
    }

    public EntityUid? GetActiveMartialArt(EntityUid target)
        => GetContainer(target)?.Comp.ActiveMartialArt;

    private void CheckGrabStageOverridePass(Entity<KnowledgeHolderComponent> ent, ref CheckGrabOverridesEvent args)
    {
        if (GetActiveMartialArt(ent) is { } uid)
            RaiseLocalEvent(uid, ref args);
    }

    private void OnSpeedModifier(Entity<KnowledgeHolderComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (GetActiveMartialArt(ent) is not { } art)
            return;
        var ev = new RefreshMovementSpeedModifiersEvent();
        RaiseLocalEvent(art, ev);
        args.ModifySpeed(ev.WalkSpeedModifier, ev.SprintSpeedModifier);
    }

    private void OnProjectileHit(Entity<KnowledgeHolderComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (GetActiveMartialArt(ent) is not { } art)
            return;
        RaiseLocalEvent(art, ref args);
    }

    private void OnComboActionClicked(PerformMartialArtComboEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var uid = args.Performer;

        // 1. Get the Knowledge entity (where the ComboActionsComponent lives)
        if (GetActiveMartialArt(uid) is not { } martialArt)
            return;

        if (!TryComp<ComboActionsComponent>(martialArt, out var comboActions))
            return;

        // 2. Map the Action ID to your Prototype ID
        // You can name your Action IDs to match your Combo IDs to make this easy
        comboActions.QueuedPrototype = args.Combo;

        Dirty(martialArt, comboActions);

        // Provide feedback
        _popup.PopupClient(Loc.GetString("martial-arts-queued", ("combo", args.Combo)), uid, uid);

        args.Handled = true; // This starts the cooldown in the UI
    }
}

/// <summary>
/// Relayed to knowledge and the active martial art when being attacked by something.
/// </summary>
[ByRefEvent]
public record struct TookDamageEvent(EntityUid Target, int Damage);

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

    private static readonly EntProtoId StrengthKnowledge = "StrengthKnowledge";
    private static readonly EntProtoId AthleticsKnowledge = "AthleticsKnowledge";
    private static readonly EntProtoId MeleeKnowledge = "MeleeKnowledge";
    private static readonly EntProtoId ToughnessKnowledge = "ToughnessKnowledge";

    private void InitializeMartialArts()
    {
        SubscribeLocalEvent<MartialArtsKnowledgeComponent, KnowledgeRemovedEvent>(OnMartialArtRemoved);

        SubscribeLocalEvent<ComboActionsComponent, KnowledgeEnabledEvent>(OnComboActionsEnabled);
        SubscribeLocalEvent<ComboActionsComponent, KnowledgeDisabledEvent>(OnComboActionsDisabled);

        SubscribeLocalEvent<KnowledgeHolderComponent, ShotAttemptedEvent>(OnShotAttempt);
        SubscribeLocalEvent<NoGunComponent, ShotAttemptedEvent>(OnShotAttemptKnowledge);
        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeInteractHandEvent>(OnInteract);
        SubscribeLocalEvent<KnowledgeHolderComponent, ComboAttackPerformedEvent>(OnComboAttackPerformed);
        SubscribeLocalEvent<KnowledgeHolderComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeStaminaDamageEvent>(OnStaminaTakeDamage);
        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<KnowledgeHolderComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<KnowledgeHolderComponent, CheckGrabOverridesEvent>(CheckGrabStageOverridePass);
        SubscribeLocalEvent<KnowledgeHolderComponent, RefreshMovementSpeedModifiersEvent>(OnSpeedModifier);
        SubscribeLocalEvent<KnowledgeHolderComponent, GetMeleeAttackRateEvent>(OnMeleeAttackModifier);
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

    private void OnShotAttempt(Entity<KnowledgeHolderComponent> ent, ref ShotAttemptedEvent args)
    {
        if (GetContainer(ent) is not { } brain ||
            brain.Comp.ActiveMartialArt is not { } martialArtUid)
            return;

        RaiseLocalEvent(martialArtUid, ref args);

        if (args.Cancelled)
            _popup.PopupClient(Loc.GetString("gun-disabled"), ent, ent);
    }

    private void OnShotAttemptKnowledge(Entity<NoGunComponent> ent, ref ShotAttemptedEvent args)
    {
        args.Cancel();
    }

    private void OnInteract(Entity<KnowledgeHolderComponent> ent, ref BeforeInteractHandEvent args)
    {
        if (ent.Owner == args.Target || !HasComp<MobStateComponent>(args.Target))
            return;

        if (GetActiveMartialArt(ent) is not { } skill)
            return;

        RaiseLocalEvent(skill, new ComboAttackPerformedEvent(ent.Owner, args.Target, ent.Owner, ComboAttackType.Hug));
    }

    public void OnComboAttackPerformed(Entity<KnowledgeHolderComponent> ent, ref ComboAttackPerformedEvent args)
    {
        if (GetActiveMartialArt(ent) is { } skill)
            RaiseLocalEvent(skill, args);
    }

    private void OnMeleeHit(Entity<KnowledgeHolderComponent> ent, ref MeleeHitEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;
        if (GetContainer(ent) is not { } brain)
            return;

        var bonus = 0f;
        if (GetKnowledge(brain, StrengthKnowledge) is { } strength)
            bonus += 3 * SharpCurve(strength);

        if (GetActiveMartialArt(ent) is { } martialArt)
        {
            var evSneakAttack = new InvokeSneakAttackSurprisedEvent();
            RaiseLocalEvent(martialArt, ref evSneakAttack);
            var evMartialDamage = new MartialArtDamageModifierEvent(ent);
            RaiseLocalEvent(martialArt, ref evMartialDamage);
        }

        args.BonusDamage += (args.BaseDamage * bonus);
    }

    private void OnStaminaTakeDamage(Entity<KnowledgeHolderComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (GetContainer(ent) is not { } brain)
            return;

        if (GetKnowledge(brain, AthleticsKnowledge) is { } athletics)
        {
            if (args.Value > 0)
                args.Value *= 1 - 0.99f * SharpCurve(athletics);
        }
        if (args.Value > 0 && _mobState.IsAlive(ent))
        {
            AddExperience(brain, AthleticsKnowledge, Math.Min((int) args.Value / 5, 10));
        }
    }

    private void OnBeforeDamageChanged(Entity<KnowledgeHolderComponent> ent, ref BeforeDamageChangedEvent args)
    {
        // most environment things like radiation should have no origin?
        if (args.Damage.GetTotal() <= 0 || args.Origin == null)
            return;

        if (GetKnowledge(ent, ToughnessKnowledge) is { } toughness && _mobState.IsAlive(ent.Owner))
        {
            args.Damage *= 1 - 0.99f * SharpCurve(toughness);
        }
    }

    private void OnDamageChanged(Entity<KnowledgeHolderComponent> ent, ref DamageChangedEvent args)
    {
        // ignore healing or things like radiation
        if (args.DamageDelta is not { } delta || !args.DamageIncreased || !args.InterruptsDoAfters ||
            // pvs can remove the brain sometimes so dont get trolled
            _timing.ApplyingState || !_timing.IsFirstTimePredicted)
            return;

        // TODO: this has fucking nothing to do with martial arts make a separate system for it
        if (_mobState.IsAlive(ent))
        {
            // to get 100 toughness you have to take 100 damage over and over... have fun
            var ev = new AddExperienceEvent(ToughnessKnowledge, Math.Min((int) delta.GetTotal() / 5, 10), delta.GetTotal().Int());
            RaiseLocalEvent(ent, ref ev);
        }
        if (GetActiveMartialArt(ent) is { } martialArt)
        {
            // TODO: bruh
            var evSneakAttack = new InvokeSneakAttackSurprisedEvent();
            RaiseLocalEvent(martialArt, ref evSneakAttack);
        }
    }

    private void OnUpdateMartialArts(KnowledgeUpdateMartialArtsEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player ||
            GetContainer(player) is not { } ent)
            return;

        var unit = ev.Knowledge is { } id
            ? GetKnowledge(ent, id)
            : null;

        if (unit != null && !HasComp<MartialArtsKnowledgeComponent>(unit))
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
            DebugTools.Assert(HasComp<MartialArtsKnowledgeComponent>(unit),
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

    private void OnMeleeAttackModifier(Entity<KnowledgeHolderComponent> ent, ref GetMeleeAttackRateEvent args)
    {
        if (GetKnowledge(ent, MeleeKnowledge) is { } melee && GetMastery(melee.Comp.Level) > 2)
        {
            // FIXME: this is too fast?
            args.Multipliers *= 1 + 2 * SharpCurve(melee, -50, 50.0f);
        }
        if (GetActiveMartialArt(ent) is not { } art)
            return;
        var ev = new GetMeleeAttackRateEvent(args.Weapon, args.Rate, args.Multipliers, args.User);
        RaiseLocalEvent(art, ref ev);
        args.Multipliers *= ev.Multipliers;
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

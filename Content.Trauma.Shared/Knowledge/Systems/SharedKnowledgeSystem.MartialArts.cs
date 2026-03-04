// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Trauma.Common.Bulletholes;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.MartialArts;
using Content.Trauma.Shared.MartialArts.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;
public abstract partial class SharedKnowledgeSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    private static readonly EntProtoId StrengthKnowledge = "StrengthKnowledge";
    private static readonly EntProtoId AthleticsKnowledge = "AthleticsKnowledge";
    private static readonly EntProtoId MeleeKnowledge = "MeleeKnowledge";
    private static readonly EntProtoId ToughnessKnowledge = "ToughnessKnowledge";

    private void InitializeMartialArts()
    {
        SubscribeLocalEvent<KnowledgeHolderComponent, ShotAttemptedEvent>(OnShotAttempt);
        SubscribeLocalEvent<NoGunComponent, ShotAttemptedEvent>(OnShotAttemptKnowledge);
        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeInteractHandEvent>(OnInteract);
        SubscribeLocalEvent<KnowledgeHolderComponent, ComboAttackPerformedEvent>(OnComboAttackPerformed);
        SubscribeLocalEvent<MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeStaminaDamageEvent>(OnStaminaTakeDamage);
        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeDamageChangedEvent>(OnTakeDamage);
        SubscribeLocalEvent<KnowledgeHolderComponent, CheckGrabOverridesEvent>(CheckGrabStageOverridePass);
        SubscribeLocalEvent<KnowledgeHolderComponent, RefreshMovementSpeedModifiersEvent>(OnSpeedModifier);
        SubscribeLocalEvent<KnowledgeHolderComponent, ProjectileReflectAttemptEvent>(OnProjectileHit);
        SubscribeLocalEvent<NoGunComponent, ProjectileReflectAttemptEvent>(OnProjectileHitMartialArt);
        SubscribeLocalEvent<PerformMartialArtComboEvent>(OnComboActionClicked);

        SubscribeNetworkEvent<KnowledgeUpdateMartialArtsEvent>(OnUpdateMartialArts);
    }

    private void OnShotAttempt(Entity<KnowledgeHolderComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledge))
            return;

        if (knowledge.MartialArtSkillUid is not { } martialArtUid || !HasComp<MartialArtsKnowledgeComponent>(martialArtUid))
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

        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        if (knowledgeContainerComp.MartialArtSkillUid is not { } martialArtSkillUid)
            return;

        RaiseLocalEvent(ent.Owner, new ComboAttackPerformedEvent(ent.Owner, args.Target, ent.Owner, ComboAttackType.Hug));
    }

    public void OnComboAttackPerformed(Entity<KnowledgeHolderComponent> ent, ref ComboAttackPerformedEvent args)
    {
        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        if (knowledgeContainerComp.MartialArtSkillUid is not { } martialArtSkillUid || !TryComp<MartialArtsKnowledgeComponent>(martialArtSkillUid, out var martialArtComp))
            return;

        RaiseLocalEvent(martialArtSkillUid, args);
    }

    private void OnMeleeHit(MeleeHitEvent args)
    {
        if (args.Handled)
            return;

        var ent = args.User;

        if (!TryComp<KnowledgeHolderComponent>(ent, out var knowledgeComp) || knowledgeComp.KnowledgeEntity == null)
            return;

        var bonus = 0f;
        if (TryGetKnowledgeUnit(ent, StrengthKnowledge) is { } strength)
        {
            bonus += 3 * SharpCurve(strength);
        }

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
        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        if (TryGetKnowledgeUnit(ent, AthleticsKnowledge) is { } athletics)
        {
            if (args.Value > 0)
                args.Value *= 1 - 0.99f * SharpCurve(athletics);
        }
        if (args.Value > 0 && _mobState.IsAlive(ent))
        {
            var ev = new AddExperienceEvent(AthleticsKnowledge, Math.Min((int) args.Value / 5, 10));
            RaiseLocalEvent(ent, ref ev);
        }
    }

    private void OnTakeDamage(Entity<KnowledgeHolderComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (TryGetKnowledgeUnit(ent, ToughnessKnowledge) is { } toughness && _mobState.IsAlive(ent.Owner))
        {
            if (args.Damage.GetTotal() > 0)
                args.Damage *= 1 - 0.99f * SharpCurve(toughness);
        }
        if (args.Damage.GetTotal() > 0 && !_mobState.IsDead(ent))
        {
            var ev = new AddExperienceEvent(ToughnessKnowledge, Math.Min((int) args.Damage.GetTotal() / 5, 10));
            RaiseLocalEvent(ent, ref ev);
        }
        if (GetActiveMartialArt(ent) is { } martialArt)
        {
            var evSneakAttack = new InvokeSneakAttackSurprisedEvent();
            RaiseLocalEvent(martialArt, ref evSneakAttack);
        }
    }

    private void OnUpdateMartialArts(KnowledgeUpdateMartialArtsEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        var knowledgeUid = GetEntity(ev.Knowledge);
        string? proto = null;
        if (knowledgeUid is { } notNullKnowledge)
            proto = Prototype(notNullKnowledge)?.ID;

        if (knowledgeUid is { } && proto is { } && TryGetKnowledgeUnit(player, proto) is { } trueKnowledge && trueKnowledge != knowledgeUid) // Anti-cheat line, if the client is trying to set a martial art they don't actually have and not null, ignore it.
            return;

        ChangeMartialArts(player, knowledgeUid);
    }

    public void ChangeMartialArts(EntityUid player, EntityUid? knowledgeUid)
    {
        if (!TryComp<KnowledgeHolderComponent>(player, out var knowledgeHolder) || TryGetKnowledgeContainer((player, knowledgeHolder)) is not { } knowledgeEnt)
            return;

        if (TryComp<ComboActionsComponent>(knowledgeEnt.Comp.MartialArtSkillUid, out var actionComp))
        {
            foreach (var (comboId, actionEntity) in actionComp.ComboActions)
            {
                _actions.RemoveAction(player, actionEntity);
            }
            actionComp.ComboActions.Clear();
        }

        knowledgeEnt.Comp.MartialArtSkillUid = knowledgeUid;

        if (TryComp<ComboActionsComponent>(knowledgeEnt.Comp.MartialArtSkillUid, out var actionCompTwo))
        {
            foreach (var (comboId, actionId) in actionCompTwo.StoredComboActions)
            {
                if (_actions.AddAction(player, actionId) is { } action)
                    actionCompTwo.ComboActions[comboId] = action;
            }
        }

        Dirty(knowledgeEnt);
    }

    private EntityUid? GetActiveMartialArt(EntityUid target)
    {
        if (TryGetKnowledgeEntity(target) is { } brain && TryComp<KnowledgeContainerComponent>(brain, out var knowledgeContainerComp) && knowledgeContainerComp.MartialArtSkillUid is { } martialArt)
            return martialArt;
        return null;
    }

    private void CheckGrabStageOverridePass(Entity<KnowledgeHolderComponent> ent, ref CheckGrabOverridesEvent args)
    {
        var martialArt = GetActiveMartialArt(ent);
        if (martialArt is { } martialArtSkillUid)
            RaiseLocalEvent(martialArtSkillUid, ref args);
    }

    private void OnSpeedModifier(Entity<KnowledgeHolderComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (GetActiveMartialArt(ent) is not { } art)
            return;
        var ev = new MartialArtSpeedModifierEvent(ent.Owner, 1.0f);
        RaiseLocalEvent(art, ref ev);
        args.ModifySpeed(ev.Coefficient);
    }

    private void OnProjectileHit(Entity<KnowledgeHolderComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (GetActiveMartialArt(ent) is not { } art)
            return;
        RaiseLocalEvent(art, ref args);
    }

    private void OnProjectileHitMartialArt(Entity<NoGunComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        args.Cancelled = true;
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

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Devour;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Item;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Polymorph.Components;
using Content.Shared.Polymorph.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.Morph;

/// <summary>
/// Handles all of morph's interactions.
/// </summary>
public sealed partial class MorphSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChameleonProjectorSystem _chameleon = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<ChameleonDisguisedComponent> _disguisedQuery = default!;
    [Dependency] private EntityQuery<HumanoidProfileComponent> _humanoidQuery = default!;
    [Dependency] private EntityQuery<ItemComponent> _itemQuery = default!;
    [Dependency] private EntityQuery<MindContainerComponent> _mindQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobQuery = default!;
    [Dependency] private EntityQuery<MorphComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MorphComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MorphComponent, MorphActionEvent>(OnMorphAction);
        SubscribeLocalEvent<MorphComponent, UnMorphActionEvent>(OnUnMorphAction);
        SubscribeLocalEvent<MorphComponent, DevourDoAfterEvent>(OnMorphDevour);
        SubscribeLocalEvent<MorphComponent, MorphReplicateActionEvent>(OnMorphReplicate);
        SubscribeLocalEvent<MorphComponent, ReplicateDoAfterEvent>(OnMorphReplicateDoAfter);
        SubscribeLocalEvent<MorphComponent, TransformSpeakerNameEvent>(OnTransformSpeakerName);
        SubscribeLocalEvent<MorphComponent, DamageChangedEvent>(OnTakeDamage);
        SubscribeLocalEvent<MorphComponent, MobStateChangedEvent>(OnDeath);
        SubscribeLocalEvent<MorphComponent, AttemptMeleeEvent>(OnAttemptMelee);

        SubscribeLocalEvent<MorphDisguiseComponent, ExaminedEvent>(OnDisguiseExamined);
    }

    private void OnMapInit(Entity<MorphComponent> ent, ref MapInitEvent args)
    {
        foreach (var action in ent.Comp.Actions)
        {
            _actions.AddAction(ent.Owner, action);
        }

        _alerts.ShowAlert(ent.Owner, ent.Comp.BiomassAlert);
    }

    private void ChangeBiomassAmount(Entity<MorphComponent> ent, FixedPoint2 amount)
    {
        ent.Comp.Biomass = FixedPoint2.Min(ent.Comp.Biomass + amount, ent.Comp.MaxBiomass);
        Dirty(ent);
    }

    private void OnMorphDevour(Entity<MorphComponent> ent, ref DevourDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not {} target)
            return;

        // !
        _chameleon.TryReveal(ent.Owner);

        var user = args.User;
        if (_whitelist.CheckBoth(target, ent.Comp.BiomassBlacklist, ent.Comp.BiomassWhitelist))
            ChangeBiomassAmount(ent, GetBiomass(target));
        else
            _popup.PopupClient(Loc.GetString("morph-no-biomass-target"), ent, user, PopupType.MediumCaution);

       // make sure the food is dead
        if (_mobQuery.TryComp(target, out var mob) && !_mob.IsDead(target, mob))
           _mob.ChangeMobState(target, MobState.Dead, mob);
    }

    private void OnMorphReplicate(Entity<MorphComponent> ent, ref MorphReplicateActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Biomass <= ent.Comp.ReplicateCost)
        {
            _popup.PopupClient("Not enough biomass!", ent, ent, PopupType.MediumCaution);
            return;
        }

        if (_disguisedQuery.HasComp(ent))
        {
            _popup.PopupClient("You can't replicate while morphed!", ent, ent, PopupType.SmallCaution);
            return;
        }

        var delay = ent.Comp.ReplicationDelay;
        var doafterArgs = new DoAfterArgs(EntityManager, ent, delay, new ReplicateDoAfterEvent(), ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.5f,
        };

        args.Handled = _doAfter.TryStartDoAfter(doafterArgs);

        if (args.Handled)
            _popup.PopupClient("You start to reproduce...", ent, ent, PopupType.Medium);
    }

    private void OnMorphReplicateDoAfter(Entity<MorphComponent> ent, ref ReplicateDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        var xform = Transform(ent);
        var coords = xform.Coordinates;
        ChangeBiomassAmount(ent, -ent.Comp.ReplicateCost);

        var spawned = PredictedSpawnAtPosition(ent.Comp.MorphPrototype, coords);
        _transform.SetLocalRotation(spawned, xform.LocalRotation);
        _audio.PlayPredicted(ent.Comp.ReplicateSound, ent, ent);
        ent.Comp.Children++;

        if (_query.TryComp(spawned, out var child))
            child.Rule = ent.Comp.Rule;
    }

    private void OnUnMorphAction(Entity<MorphComponent> ent, ref UnMorphActionEvent args)
    {
        _chameleon.TryReveal(ent.Owner);
    }

    private void OnDisguiseExamined(Entity<MorphDisguiseComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
            args.PushMarkup(Loc.GetString(ent.Comp.ExamineMessage), ent.Comp.ExaminePriority);
    }

    private void OnTakeDamage(Entity<MorphComponent> ent, ref DamageChangedEvent args)
    {
        if (args.Origin == null || // ignore radiation etc
            args.DamageDelta is not {} delta ||
            !args.DamageIncreased || // ignore healing
            delta.GetTotal() < ent.Comp.DamageThreshold)
            return;

        // damage is over threshold, reveal if morphed
        _chameleon.TryReveal(ent.Owner);
    }

    private void OnDeath(Entity<MorphComponent> ent, ref MobStateChangedEvent args)
    {
        // remove disguise in case morph dies while in disguise
        if (args.NewMobState is MobState.Dead)
            _chameleon.TryReveal(ent.Owner);
    }

    private void OnTransformSpeakerName(Entity<MorphComponent> ent, ref TransformSpeakerNameEvent args)
    {
        if (!_disguisedQuery.TryComp(ent, out var comp))
            return;

        // appear to speak as the diguise
        args.VoiceName = Name(comp.Disguise);
        args.Sender = comp.Disguise;
    }

    private void OnAttemptMelee(Entity<MorphComponent> ent, ref AttemptMeleeEvent args)
    {
        if (!_disguisedQuery.HasComp(ent))
            return;

        _popup.PopupClient("You can't attack while morphed!", ent, ent);
        args.Cancelled = true;
    }

    private void OnMorphAction(Entity<MorphComponent> ent, ref MorphActionEvent args)
    {
        _chameleon.TryDisguise((ent, Comp<ChameleonProjectorComponent>(ent)), ent, args.Target);
        // TODO: completely copy inventory etc sprite layers
    }

    /// <summary>
    /// Get the biomass gained by devouring an entity.
    /// </summary>
    public FixedPoint2 GetBiomass(EntityUid uid)
    {
        if (!_mobQuery.HasComp(uid))
            return 0; // you can eat items just to be annoying

        if (_itemQuery.HasComp(uid))
            return 5; // tiny mobs like mice, maybe a pAI

        if (!_humanoidQuery.HasComp(uid))
            return 10; // non-humanoids like pets

        // player-controlled humanoids give 30, catatonic give 20
        return _mindQuery.TryComp(uid, out var mind) && mind.HasMind ? 30 : 20;
    }
}

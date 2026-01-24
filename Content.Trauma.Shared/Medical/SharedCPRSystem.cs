// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared._Shitmed.Medical.Surgery.Traumas;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Traits.Assorted;
using Content.Shared.Verbs;
using Content.Trauma.Common.Body;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Medical;

public abstract class SharedCPRSystem : EntitySystem
{
    [Dependency] private readonly IngestionSystem _ingestion = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <summary>
    /// Modifier for inhale volume on mobs that have CPR being done on them.
    /// </summary>
    public const float InhaleModifier = 3f;

    private EntityQuery<ActiveCPRComponent> _activeQuery;
    private EntityQuery<CPRTrainingComponent> _trainingQuery;
    private EntityQuery<DamageableComponent> _damageQuery;
    private EntityQuery<MobStateComponent> _mobQuery;
    private EntityQuery<OrganComponent> _organQuery;
    private EntityQuery<RottingComponent> _rottingQuery;
    private EntityQuery<UnrevivableComponent> _unrevivableQuery;

    public override void Initialize()
    {
        base.Initialize();

        _activeQuery = GetEntityQuery<ActiveCPRComponent>();
        _trainingQuery = GetEntityQuery<CPRTrainingComponent>();
        _damageQuery = GetEntityQuery<DamageableComponent>();
        _mobQuery = GetEntityQuery<MobStateComponent>();
        _organQuery = GetEntityQuery<OrganComponent>();
        _rottingQuery = GetEntityQuery<RottingComponent>();
        _unrevivableQuery = GetEntityQuery<UnrevivableComponent>();

        SubscribeLocalEvent<CPRTrainingComponent, GetVerbsEvent<InnateVerb>>(OnGetVerbs);

        SubscribeLocalEvent<ActiveCPRComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ActiveCPRComponent, CPRDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ActiveCPRComponent, ModifyInhaledVolumeEvent>(OnModifyInhaledVolume);
    }

    /// <summary>
    /// Returns true if a mob is having CPR done on it.
    /// </summary>
    public bool IsCPRActive(EntityUid uid)
        => _activeQuery.HasComp(uid);

    private void OnGetVerbs(Entity<CPRTrainingComponent> ent, ref GetVerbsEvent<InnateVerb> args)
    {
        var target = args.Target;
        if (!args.CanInteract || !args.CanAccess || !_mobQuery.TryComp(target, out var mob) || mob.CurrentState == MobState.Alive)
            return;

        args.Verbs.Add(new InnateVerb()
        {
            Act = () => StartCPR(ent, target),
            Text = Loc.GetString("cpr-verb"),
            Icon = new SpriteSpecifier.Rsi(new("Interface/Alerts/human_alive.rsi"), "health4"),
            Priority = 2
        });
    }

    private void StartCPR(Entity<CPRTrainingComponent> ent, EntityUid target)
    {
        var identity = Identity.Entity(target, EntityManager);
        if (!CanStartCPR(ent, target, identity))
            return;

        var userIdentity = Identity.Entity(ent, EntityManager);
        _popup.PopupClient(Loc.GetString("cpr-start-second-person", ("target", identity)), target, ent);
        _popup.PopupEntity(Loc.GetString("cpr-start-second-person-patient", ("user", userIdentity)), target, target);

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            ent,
            ent.Comp.Duration,
            new CPRDoAfterEvent(),
            eventTarget: target,
            target: target)
        {
            BreakOnMove = true,
            BreakOnHandChange = true,
            NeedHand = true,
            BlockDuplicate = true
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        var active = EnsureComp<ActiveCPRComponent>(target);
        // PlayPredicted is shitcode and doesnt spawn the same entity for client, can't do it nicely
        if (_net.IsClient || _audio.PlayPredicted(ent.Comp.Sound, ent, ent, ent.Comp.Sound.Params.WithLoop(true)) is not {} audio)
            return;

        active.Sound = audio.Entity;
        Dirty(target, active);
    }

    private bool CanStartCPR(Entity<CPRTrainingComponent> ent, EntityUid target, EntityUid identity)
    {
        if (_activeQuery.HasComp(target))
        {
            _popup.PopupClient(Loc.GetString("cpr-already-performing", ("entity", identity)), ent, ent, PopupType.Medium);
            return false;
        }

        return CanPerformCPR(ent, target, identity);
    }

    private bool CanPerformCPR(Entity<CPRTrainingComponent> ent, EntityUid target, EntityUid identity)
    {
        if (_rottingQuery.HasComp(target))
        {
            _popup.PopupClient(Loc.GetString("cpr-target-rotting", ("entity", identity)), ent, ent, PopupType.LargeCaution);
            return false;
        }

        if (GetLungs(target) == null || GetLungs(ent) == null)
        {
            _popup.PopupClient(Loc.GetString("cpr-target-cantbreathe", ("entity", identity)), ent, ent, PopupType.MediumCaution);
            return false;
        }

        if (_inventory.TryGetSlotEntity(target, "outerClothing", out var outer))
        {
            _popup.PopupClient(Loc.GetString("cpr-must-remove", ("clothing", outer)), ent, ent, PopupType.Medium);
            return false;
        }

        // popups done in ingestion system
        return _ingestion.HasMouthAvailable(ent, ent) || !_ingestion.HasMouthAvailable(ent, target);
    }

    private void OnShutdown(Entity<ActiveCPRComponent> ent, ref ComponentShutdown args)
    {
        _audio.Stop(ent.Comp.Sound);
    }

    private void OnDoAfter(Entity<ActiveCPRComponent> ent, ref CPRDoAfterEvent args)
    {
        var user = args.User;
        if (args.Cancelled || args.Handled)
        {
            RemCompDeferred(ent, ent.Comp);
            return;
        }

        args.Handled = true;

        var identity = Identity.Entity(ent, EntityManager);
        if (!_trainingQuery.TryComp(user, out var training) ||
            !_mobQuery.TryComp(ent, out var mob) ||
            !CanPerformCPR((user, training), ent, identity))
        {
            RemCompDeferred(ent, ent.Comp);
            return;
        }

        if (!HasHealthyLungs(ent))
        {
            _popup.PopupClient(Loc.GetString("cpr-failed-lungs-damaged", ("target", identity)), user, user, PopupType.LargeCaution);
            RemCompDeferred(ent, ent.Comp);
            return;
        }

        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);
        if (mob.CurrentState == MobState.Dead && rand.Prob(training.ReviveChance) && CanRevive(ent))
            _mob.ChangeMobState(ent.Owner, MobState.Critical, origin: user);

        if (rand.Prob(training.InhaleChance) && HasHealthyLungs(ent))
            TryInhale(ent); // technically should be transferring with the performer's lungs but whatever

        var isAlive = mob.CurrentState == MobState.Alive;
        args.Repeat = !isAlive;
        if (isAlive)
            RemCompDeferred(ent, ent.Comp);
    }

    private void OnModifyInhaledVolume(Entity<ActiveCPRComponent> ent, ref ModifyInhaledVolumeEvent args)
    {
        args.Volume *= InhaleModifier; // you are being assisted
    }

    private bool HasHealthyLungs(EntityUid uid)
        // need healthy lungs for CPR to work, go tend organ damage first
        => GetLungs(uid) is {} lungs &&
            _organQuery.TryComp(lungs, out var organ) &&
            organ.OrganSeverity == OrganSeverity.Normal;

    private bool CanRevive(EntityUid uid)
        // no cpr major reviving ling husks
        => !_unrevivableQuery.HasComp(uid) &&
            // has to be below death threshold
            _threshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold) &&
            _damageQuery.TryComp(uid, out var damage) &&
            _threshold.CheckVitalDamage(uid, damage) < threshold;

    private EntityUid? GetLungs(EntityUid mob)
    {
        foreach (var organ in _body.GetBodyOrganEntityComps<LungComponent>(mob))
        {
            return organ;
        }

        return null;
    }

    // respiration is serverside :(
    protected virtual void TryInhale(EntityUid uid)
    {
    }
}

[Serializable, NetSerializable]
public sealed partial class CPRDoAfterEvent : SimpleDoAfterEvent;

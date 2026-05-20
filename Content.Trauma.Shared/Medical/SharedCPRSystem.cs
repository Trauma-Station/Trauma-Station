// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Medical.Common.Traumas;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Traits.Assorted;
using Content.Shared.Verbs;
using Content.Trauma.Common.Body;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Medical;

public abstract partial class SharedCPRSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IngestionSystem _ingestion = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<ActiveCPRComponent> _activeQuery = default!;
    [Dependency] private EntityQuery<DamageableComponent> _damageQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobQuery = default!;
    [Dependency] private EntityQuery<InternalOrganComponent> _organQuery = default!;
    [Dependency] private EntityQuery<RottingComponent> _rottingQuery = default!;
    [Dependency] private EntityQuery<UnrevivableComponent> _unrevivableQuery = default!;

    /// <summary>
    /// Modifier for inhale volume on mobs that have CPR being done on them.
    /// </summary>
    public const float InhaleModifier = 3f;

    public static readonly ProtoId<OrganCategoryPrototype> LungsCategory = "Lungs";

    /// <summary>
    /// Sound to play on chest compressions
    /// </summary>
    private SoundSpecifier _sound = new SoundPathSpecifier("/Audio/_EinsteinEngines/Effects/CPR.ogg");

    private TimeSpan _cprTime = TimeSpan.FromSeconds(4);

    private static readonly EntProtoId FirstAidKnowledge = "FirstAidKnowledge";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, GetVerbsEvent<InnateVerb>>(OnGetVerbs);

        SubscribeLocalEvent<ActiveCPRComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ActiveCPRComponent, CPRDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ActiveCPRComponent, ModifyInhaledVolumeEvent>(OnModifyInhaledVolume);
    }

    /// <summary>
    /// Returns true if a mob is having CPR done on it.
    /// </summary>
    public bool IsCPRActive(EntityUid uid)
        => _activeQuery.HasComp(uid);

    private void OnGetVerbs(Entity<KnowledgeHolderComponent> ent, ref GetVerbsEvent<InnateVerb> args)
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

    private void StartCPR(Entity<KnowledgeHolderComponent> ent, EntityUid target)
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
            _cprTime,
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
        if (_net.IsClient || _audio.PlayPredicted(_sound, ent, ent, _sound.Params.WithLoop(true)) is not { } audio)
            return;

        active.Sound = audio.Entity;
        Dirty(target, active);
    }

    private bool CanStartCPR(Entity<KnowledgeHolderComponent> ent, EntityUid target, EntityUid identity)
    {
        if (_activeQuery.HasComp(target))
        {
            _popup.PopupClient(Loc.GetString("cpr-already-performing", ("entity", identity)), ent, ent, PopupType.Medium);
            return false;
        }

        return CanPerformCPR(ent, target, identity);
    }

    private bool CanPerformCPR(EntityUid uid, EntityUid target, EntityUid identity)
    {
        if (_rottingQuery.HasComp(target))
        {
            _popup.PopupClient(Loc.GetString("cpr-target-rotting", ("entity", identity)), uid, uid, PopupType.LargeCaution);
            return false;
        }

        if (GetLungs(target) == null || GetLungs(uid) == null)
        {
            _popup.PopupClient(Loc.GetString("cpr-target-cantbreathe", ("entity", identity)), uid, uid, PopupType.MediumCaution);
            return false;
        }

        if (_inventory.TryGetSlotEntity(target, "outerClothing", out var outer))
        {
            _popup.PopupClient(Loc.GetString("cpr-must-remove", ("clothing", outer)), uid, uid, PopupType.Medium);
            return false;
        }

        // popups done in ingestion system
        return _ingestion.HasMouthAvailable(uid, uid) || !_ingestion.HasMouthAvailable(uid, target);
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
        if (!_mobQuery.TryComp(ent, out var mob) || !CanPerformCPR(user, ent, identity))
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

        if (_knowledge.GetContainer(user) is not { } brain || _knowledge.GetSkill(brain, FirstAidKnowledge) is not { } firstAid) // Gotta make sure you can actually do this lmao.
            return;

        if (mob.CurrentState == MobState.Dead && CanRevive(ent))
        {
            var contestReviveEv = new SingleContestEvent(100, 40, firstAid.Comp.NetLevel, true); // Reviving someone should be much harder
            RaiseLocalEvent(user, ref contestReviveEv);
            if (!contestReviveEv.Failed)
                _mob.ChangeMobState(ent.Owner, MobState.Critical, origin: user);

            _knowledge.AddExperience(brain, FirstAidKnowledge, 10);
        }

        if (HasHealthyLungs(ent))
        {
            var contestInhaleEv = new SingleContestEvent(100, 15, firstAid.Comp.NetLevel); // Breath inhaltion is easier
            RaiseLocalEvent(user, ref contestInhaleEv);
            if (!contestInhaleEv.Failed)
                TryInhale(ent); // technically should be transferring with the performer's lungs but whatever
            _knowledge.AddExperience(brain, FirstAidKnowledge, 3);
        }

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
        => GetLungs(uid) is { } lungs &&
            _organQuery.TryComp(lungs, out var organ) &&
            organ.OrganSeverity == OrganSeverity.Normal;

    private bool CanRevive(EntityUid uid)
        // no cpr major reviving ling husks
        => !_unrevivableQuery.HasComp(uid) &&
            // has to be below death threshold
            _threshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold) &&
            _damageQuery.TryComp(uid, out var damage) &&
            _threshold.CheckVitalDamage((uid, damage)) < threshold;

    private EntityUid? GetLungs(EntityUid mob)
        => _body.GetOrgan(mob, LungsCategory);

    // respiration is serverside :(
    protected virtual void TryInhale(EntityUid uid)
    {
    }
}

[Serializable, NetSerializable]
public sealed partial class CPRDoAfterEvent : SimpleDoAfterEvent;

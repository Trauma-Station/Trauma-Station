using Content.Server.Antag;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._DV.CosmicCult.EntitySystems;

public sealed class CosmicChantrySystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;

    /// <summary>
    /// Mind role to add to colossi.
    /// </summary>
    public static readonly EntProtoId MindRole = "MindRoleCosmicColossus";
    private readonly SoundSpecifier _briefingSound = new SoundPathSpecifier("/Audio/_DV/CosmicCult/antag_cosmic_AI_briefing.ogg");
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicChantryComponent, ComponentStartup>(OnChantryStarted);
        SubscribeLocalEvent<CosmicChantryComponent, DestructionEventArgs>(OnChantryDestroyed);
        SubscribeLocalEvent<CosmicChantryComponent, CosmicChantryDoAfter>(OnDoAfter);
        //SubscribeLocalEvent<CosmicChantryVictimComponent, MindRemovedMessage>(OnMindLeftVictim); //TODO: automatically make the contained posibrain a ghost role to prevent shitters ghosting
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var chantryQuery = EntityQueryEnumerator<CosmicChantryComponent>();
        while (chantryQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.Container.Count <= 1 && comp.Victim != default!) // Doing this on component startup doesn't put borg into the container properly so we do it on next update instead
                _containerSystem.Insert(comp.Victim, comp.Container);
            if (_timing.CurTime >= comp.SpawnTimer && !comp.Spawned)
            {
                _appearance.SetData(uid, ChantryVisuals.Status, ChantryStatus.On);
                _popup.PopupCoordinates(Loc.GetString("cosmiccult-chantry-powerup"), Transform(uid).Coordinates, PopupType.LargeCaution);
                comp.Spawned = true;
                
                if (!_threshold.TryGetThresholdForState(comp.Victim, MobState.Critical, out var damage)
                || !TryComp<DamageableComponent>(comp.Victim, out var damageable)
                || damage < _damage.GetDamage((comp.Victim, damageable)).GetTotal())
                    return;
                damage -= _damage.GetDamage((comp.Victim, damageable)).GetTotal();
                if (damage <= 0)
                    return;
                DamageSpecifier dspec = new();
                dspec.DamageDict.Add("Slash", damage.Value);
                _damage.TryChangeDamage(comp.Victim, dspec, true);
                
                var doAfterArgs = new DoAfterArgs(EntityManager, uid, comp.EventTime, new CosmicChantryDoAfter(), uid, comp.Victim)
                {
                    NeedHand = false,
                    BreakOnWeightlessMove = false,
                    BreakOnMove = false,
                    BreakOnHandChange = false,
                    BreakOnDropItem = false,
                    BreakOnDamage = false,
                    RequireCanInteract = false,
                };
                _doAfter.TryStartDoAfter(doAfterArgs);
            }
        }
    }

    private void OnDoAfter(Entity<CosmicChantryComponent> ent, ref CosmicChantryDoAfter args)
    {
        if (!_mind.TryGetMind(ent.Comp.Victim, out var mindEnt, out var mind))
            return;
        var tgtpos = Transform(ent).Coordinates;
        var colossus = Spawn(ent.Comp.Colossus, tgtpos);
        _mind.TransferTo(mindEnt, colossus);
        _mind.TryAddObjective(mindEnt, mind, "CosmicFinalityObjective");
        _role.MindAddRole(mindEnt, MindRole, mind, true);
        _antag.SendBriefing(colossus, Loc.GetString("cosmiccult-silicon-colossus-briefing"), Color.FromHex("#4cabb3"), null);
        Spawn(ent.Comp.SpawnVFX, tgtpos);

        _containerSystem.EmptyContainer(ent.Comp.Container);
        if (TryComp<CosmicColossusComponent>(colossus, out var colossusComp))
        {
            colossusComp.Container = _containerSystem.EnsureContainer<Container>(colossus, colossusComp.ContainerId);
            _containerSystem.Insert(ent.Comp.Victim, colossusComp.Container);
            colossusComp.ImprisonedEntity = ent.Comp.Victim;
        }

        QueueDel(ent);
    }

    private void OnChantryStarted(Entity<CosmicChantryComponent> ent, ref ComponentStartup args)
    {
        var comp = ent.Comp;
        var indicatedLocation = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((ent, Transform(ent))));
        comp.Container = _containerSystem.EnsureContainer<Container>(ent, comp.ContainerId);
        comp.SpawnTimer = _timing.CurTime + comp.SpawningTime;
        _sound.PlayGlobalOnStation(ent, _audio.ResolveSound(comp.ChantryAlarm));
        _chatSystem.DispatchStationAnnouncement(ent,
        Loc.GetString("cosmiccult-chantry-location", ("location", indicatedLocation)),
        null, false, null,
        Color.FromHex("#cae8e8"));
    }

    private void OnChantryDestroyed(Entity<CosmicChantryComponent> ent, ref DestructionEventArgs args)
    {
        var comp = ent.Comp;
        _containerSystem.EmptyContainer(comp.Container);
        _sound.PlayGlobalOnStation(ent, _audio.ResolveSound(comp.ChantryDestructionAnnouncement));
        _chatSystem.DispatchStationAnnouncement(ent,
        Loc.GetString("cosmiccult-chantry-destruction"),
        null, false, null,
        Color.FromHex("#cae8e8"));
    }

    //TODO: automatically make the contained posibrain a ghost role to prevent shitters ghosting
    //private void OnMindLeftVictim(Entity<CosmicChantryVictimComponent> ent, ref MindRemovedMessage args)
    //{
    //}
}

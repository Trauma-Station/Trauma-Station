using Content.Server.Antag;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Shared._CorvaxNext.Silicons.Borgs.Components;
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
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
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
    [Dependency] private readonly CosmicCultRuleSystem _cultRule = default!;
    [Dependency] private readonly EntityManager _entMan = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Mind role to add to colossi.
    /// </summary>
    public static readonly EntProtoId MindRole = "MindRoleCosmicColossus";
    private readonly SoundSpecifier _briefingSound = new SoundPathSpecifier("/Audio/_DV/CosmicCult/antag_cosmic_AI_briefing.ogg");
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicChantryComponent, DestructionEventArgs>(OnChantryDestroyed);
        SubscribeLocalEvent<CosmicChantryComponent, CosmicChantryDoAfter>(OnDoAfter);
        SubscribeLocalEvent<CosmicChantryVictimComponent, MindRemovedMessage>(OnMindLeftVictim);
        SubscribeLocalEvent<CosmicChantryVictimComponent, MindAddedMessage>(OnMindAddedToVictim);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var chantryQuery = EntityQueryEnumerator<CosmicChantryComponent>();
        while (chantryQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.Victim is not { } victim) continue;
            if (!HasComp<CosmicChantryVictimComponent>(victim))
            { // Doing most of this on component startup doesn't work properly so we do it on next update instead. There's probably an event for this but idk.
                comp.SpawnTimer = _timing.CurTime + comp.SpawningTime;
                var indicatedLocation = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((uid, Transform(uid))));
                _sound.PlayGlobalOnStation(uid, _audio.ResolveSound(comp.ChantryAlarm));
                _chatSystem.DispatchStationAnnouncement(uid,
                Loc.GetString("cosmiccult-chantry-location", ("location", indicatedLocation)),
                null, false, null,
                Color.FromHex("#cae8e8"));

                EnsureComp<CosmicChantryVictimComponent>(victim, out var victimComp);
                victimComp.Chantry = (uid, comp);
                if (_cultRule.AssociatedGamerule(uid) is { } cult) cult.Comp.ActiveChantry = uid;

                if (_threshold.TryGetThresholdForState(victim, MobState.Critical, out var damage) && TryComp<DamageableComponent>(victim, out var damageable) && damage > _damage.GetDamage((victim, damageable)).GetTotal())
                {
                    damage -= _damage.GetDamage((victim, damageable)).GetTotal();
                    DamageSpecifier dspec = new();
                    dspec.DamageDict.Add("Slash", damage.Value);
                    _damage.TryChangeDamage(victim, dspec, true);
                }

                if (!TryComp<BorgChassisComponent>(victim, out var borgComp) || borgComp.BrainEntity is not { } borgBrain) return;
                var newBrain = Spawn(comp.Mindsink);
                _containerSystem.EmptyContainer(borgComp.BrainContainer);
                if (HasComp<AiRemoteBrainComponent>(borgBrain))
                { // B.O.R.I.S gets yeeted, don't want to trap the AI inside a colossus
                    _containerSystem.TryRemoveFromContainer(borgBrain, force: true);
                    _throw.TryThrow(borgBrain, _random.NextVector2(2f, 3f), baseThrowSpeed: 10, uid, 0, 0, false, false);
                    _containerSystem.Insert(newBrain, borgComp.BrainContainer);
                    MakeVictimGhostRole(victim);
                }
                else
                { // Anything else just gets fully replaced with a mindsink
                    if (_mind.TryGetMind(borgBrain, out var mindEnt, out _))
                        _mind.TransferTo(mindEnt, newBrain);
                    else
                        MakeVictimGhostRole(victim);
                    _containerSystem.Insert(newBrain, borgComp.BrainContainer);
                    QueueDel(borgBrain);
                }
            }
            if (_timing.CurTime >= comp.SpawnTimer && !comp.Spawned)
            {
                _appearance.SetData(uid, ChantryVisuals.Status, ChantryStatus.On);
                _popup.PopupCoordinates(Loc.GetString("cosmiccult-chantry-powerup"), Transform(uid).Coordinates, PopupType.LargeCaution);
                comp.Spawned = true;

                var doAfterArgs = new DoAfterArgs(EntityManager, uid, comp.EventTime, new CosmicChantryDoAfter(), uid, victim)
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
            if (_entMan.IsQueuedForDeletion(uid))
                _containerSystem.EmptyContainer(comp.Container); // Try prevent the borg from getting deleted because the event sometimes fails mysteriously.
        }
    }

    private void OnDoAfter(Entity<CosmicChantryComponent> ent, ref CosmicChantryDoAfter args)
    {
        ent.Comp.Completed = true;
        TransformVictim(ent);
    }

    private void OnChantryDestroyed(Entity<CosmicChantryComponent> ent, ref DestructionEventArgs args)
    {
        _containerSystem.EmptyContainer(ent.Comp.Container);
        _sound.PlayGlobalOnStation(ent, _audio.ResolveSound(ent.Comp.ChantryDestructionAnnouncement));
        _chatSystem.DispatchStationAnnouncement(ent,
        Loc.GetString("cosmiccult-chantry-destruction"),
        null, false, null,
        Color.FromHex("#cae8e8"));
        if (ent.Comp.Victim is not { } victim) return;
        UnGhostRoleVictim(victim);
        RemComp<CosmicChantryVictimComponent>(victim);
    }

    /// <summary>
    /// Turn the cyborg inside the given chantry into a colossus, then delete the chantry.
    /// </summary>
    private void TransformVictim(Entity<CosmicChantryComponent> ent)
    {
        if (ent.Comp.Victim is not { } victim) return;
        if (!_mind.TryGetMind(victim, out var mindEnt, out var mind))
        {
            MakeVictimGhostRole(victim);
            return;
        }
        UnGhostRoleVictim(victim);
        var tgtpos = Transform(ent).Coordinates;
        var colossus = Spawn(ent.Comp.Colossus, tgtpos);
        _mind.TransferTo(mindEnt, colossus);
        _mind.TryAddObjective(mindEnt, mind, "ColossusFinalityObjective");
        _role.MindAddRole(mindEnt, MindRole, mind, true);
        _antag.SendBriefing(colossus, Loc.GetString("cosmiccult-silicon-colossus-briefing"), Color.FromHex("#4cabb3"), ent.Comp.BriefingSfx);
        Spawn(ent.Comp.SpawnVFX, tgtpos);
        RemComp<CosmicChantryVictimComponent>(victim);

        _containerSystem.EmptyContainer(ent.Comp.Container);
        if (TryComp<CosmicColossusComponent>(colossus, out var colossusComp))
        {
            colossusComp.Container = _containerSystem.EnsureContainer<ContainerSlot>(colossus, colossusComp.ContainerId);
            _containerSystem.Insert(victim, colossusComp.Container);
        }

        QueueDel(ent);
    }

    /// <summary>
    /// If the borg has no mind for whatever reason, make the borg brain a ghost role.
    /// </summary>
    private void MakeVictimGhostRole(EntityUid ent)
    {
        if (TryComp<CosmicChantryVictimComponent>(ent, out var victimComp))
        {
            victimComp.WasGhostRole = HasComp<GhostRoleComponent>(ent);
            victimComp.WasGhostTakeoverAvailable = HasComp<GhostTakeoverAvailableComponent>(ent);
        }
        if (!TryComp<BorgChassisComponent>(ent, out var borgComp) || borgComp.BrainEntity is not { } borgBrain) return;
        EnsureComp<GhostRoleComponent>(borgBrain, out var ghostRole);
        EnsureComp<GhostTakeoverAvailableComponent>(borgBrain);
        ghostRole.RoleName = Loc.GetString("ghost-role-information-chantry-victim-name");
        ghostRole.RoleDescription = Loc.GetString("ghost-role-information-chantry-victim-description");
        ghostRole.RoleRules = Loc.GetString("ghost-role-information-silicon-rules");
    }

    private void UnGhostRoleVictim(EntityUid ent)
    {
        if (!TryComp<BorgChassisComponent>(ent, out var borgComp) || borgComp.BrainEntity is not { } borgBrain) return;
        if (TryComp<CosmicChantryVictimComponent>(ent, out var victimComp))
        {
            if (!victimComp.WasGhostRole) RemComp<GhostRoleComponent>(borgBrain);
            if (!victimComp.WasGhostTakeoverAvailable) RemComp<GhostTakeoverAvailableComponent>(borgBrain);
        }
        else
        {
            RemComp<GhostRoleComponent>(borgBrain);
            RemComp<GhostTakeoverAvailableComponent>(borgBrain);
        }
    }

    private void OnMindLeftVictim(Entity<CosmicChantryVictimComponent> ent, ref MindRemovedMessage args) =>
        MakeVictimGhostRole(ent);

    private void OnMindAddedToVictim(Entity<CosmicChantryVictimComponent> ent, ref MindAddedMessage args)
    {
        if (!ent.Comp.Chantry.Comp.Completed) return;
        TransformVictim(ent.Comp.Chantry);
    }
}

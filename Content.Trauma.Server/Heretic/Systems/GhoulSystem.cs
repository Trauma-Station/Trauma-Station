// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Religion;
using Content.Goobstation.Shared.Religion;
using Content.Goobstation.Shared.Religion.Nullrod;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Wounds;
using Content.Server.Antag;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Hands.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Polymorph.Systems;
using Content.Server.Roles;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration.Systems;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.CombatMode;
using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Gibbing;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Species.Components;
using Content.Trauma.Common.CollectiveMind;
using Content.Trauma.Server.Chaplain;
using Content.Trauma.Shared.Chaplain.Components;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Flesh;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Prototypes;
using Content.Trauma.Shared.Heretic.Systems;
using Content.Trauma.Shared.Heretic.Systems.Abilities;
using Content.Trauma.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization.Manager;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed class GhoulSystem : SharedGhoulSystem
{
    private static readonly ProtoId<HTNCompoundPrototype> Compound = "HereticSummonCompound";
    private static readonly EntProtoId<MindRoleComponent> GhoulRole = "MindRoleGhoul";

    private static readonly ProtoId<ComponentRegistryPrototype> ComponentsToRemoveOnGhoulify =
        "ComponentsToRemoveOnGhoulify";

    private static readonly ProtoId<ComponentRegistryPrototype> ComponentsToRemoveOnUnGhoulify =
        "ComponentsToRemoveOnUnGhoulify";

    [Dependency] private readonly ISerializationManager _seriMan = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly StorageSystem _storage = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;
    [Dependency] private readonly HolyFlammableSystem _holyFlam = default!;
    [Dependency] private readonly HumanoidProfileSystem _humanoid = default!;
    [Dependency] private readonly BodySystem _body = default!;

    [Dependency] private readonly EntityQuery<BrainComponent> _brainQuery = default!;
    [Dependency] private readonly EntityQuery<WoundableComponent> _woundableQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(HolyFlammableSystem));
        SubscribeLocalEvent<GhoulComponent, MapInitEvent>(OnGhoulInit, after: [typeof(InitialBodySystem)]);
        SubscribeLocalEvent<GhoulComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<GhoulComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<GhoulComponent, MobStateChangedEvent>(OnMobStateChange);
        SubscribeLocalEvent<GhoulComponent, SetGhoulBoundHereticEvent>(OnBound);
        SubscribeLocalEvent<GhoulComponent, UserShouldTakeHolyEvent>(OnShouldTakeHoly);

        SubscribeLocalEvent<GhoulRoleComponent, GetBriefingEvent>(OnGetBriefing);

        SubscribeLocalEvent<GhoulWeaponComponent, ExaminedEvent>(OnWeaponExamine);

        SubscribeLocalEvent<HereticMinionComponent, AttackAttemptEvent>(OnTryAttack);
        SubscribeLocalEvent<HereticMinionComponent, TakeGhostRoleEvent>(OnTakeGhostRole);

        SubscribeLocalEvent<ShatteredRisenComponent, MapInitEvent>(OnRisenMapInit, after: [typeof(InitialBodySystem)]);
        SubscribeLocalEvent<ShatteredRisenComponent, HandCountChangedEvent>(OnHandCountChanged);
    }

    private void OnShouldTakeHoly(Entity<GhoulComponent> ent, ref UserShouldTakeHolyEvent args)
    {
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.WeakToHoly = true;
        args.ShouldTakeHoly = true;
    }

    private void OnBound(Entity<GhoulComponent> ent, ref SetGhoulBoundHereticEvent args)
    {
        SetBoundHeretic(ent.Owner, args.Heretic, args.Ritual);
    }

    private void OnHandCountChanged(Entity<ShatteredRisenComponent> ent, ref HandCountChangedEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        RefreshShatteredHands(ent);
    }

    private void OnRisenMapInit(Entity<ShatteredRisenComponent> ent, ref MapInitEvent args)
    {
        RefreshShatteredHands(ent);
    }

    // This is stinky but idk how to make it more sane. Shattered risen should have its hands always blocked by its 2 types of weapons
    private void RefreshShatteredHands(Entity<ShatteredRisenComponent> ent)
    {
        if (!TryComp(ent, out HandsComponent? hands) || hands.Count == 0)
            return;

        var handsEnt = (ent, hands);

        var hasWeapon1 = false;

        foreach (var held in _hands.EnumerateHeld(handsEnt))
        {
            var proto = Prototype(held);
            if (proto == null)
            {
                DropOrDelete();
                continue;
            }

            if (proto == ent.Comp.Weapon1)
                hasWeapon1 = true;
            else if (proto != ent.Comp.Weapon2)
                DropOrDelete();

            continue;

            void DropOrDelete()
            {
                if (!_hands.TryDrop(handsEnt, held, null, false, false))
                    QueueDel(held);
            }
        }

        var coords = Transform(ent).Coordinates;

        foreach (var hand in _hands.EnumerateHands(handsEnt))
        {
            if (_hands.TryGetHeldItem(handsEnt, hand, out _))
                continue;

            var toSpawn = ent.Comp.Weapon1;
            if (!hasWeapon1)
                hasWeapon1 = true;
            else
                toSpawn = ent.Comp.Weapon2;

            var weapon = Spawn(toSpawn, coords);
            if (!_hands.TryForcePickup(handsEnt, weapon, hand, false, false, hands))
                QueueDel(weapon);
        }
    }

    private void OnGetBriefing(Entity<GhoulRoleComponent> ent, ref GetBriefingEvent args)
    {
        var uid = args.Mind.Comp.OwnedEntity;

        if (!TryComp(uid, out HereticMinionComponent? minion))
            return;

        var start = Loc.GetString("heretic-ghoul-briefing-start-noname");
        var master = minion.BoundHeretic;

        if (Exists(master))
        {
            start = Loc.GetString("heretic-ghoul-briefing-start",
                ("ent", Identity.Entity(master.Value, EntityManager)));
        }

        args.Append(start);
        args.Append(Loc.GetString("heretic-ghoul-briefing-end"));
    }

    private void OnWeaponExamine(Entity<GhoulWeaponComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.ExamineMessage));
    }

    public void SetBoundHeretic(Entity<HereticMinionComponent?, HTNComponent?> ent,
        EntityUid heretic,
        EntityUid? ritual = null,
        bool dirty = true)
    {
        if (_heretic.TryGetHereticComponent(heretic, out var comp, out _))
            comp.Minions.Add(ent);

        if (!Resolve(ent, ref ent.Comp1, false))
            ent.Comp1 = AddComp<HereticMinionComponent>(ent);

        ent.Comp1.CreationRitual ??= ritual;
        ent.Comp1.BoundHeretic = heretic;
        _npc.SetBlackboard(ent, NPCBlackboard.FollowTarget, heretic.ToCoordinates(), ent.Comp2);

        if (dirty)
            Dirty(ent, ent.Comp1);
    }

    public void UnGhoulifyEntity(Entity<GhoulComponent> ent)
    {
        if (!TryComp(ent, out HumanoidProfileComponent? humanoid))
        {
            if (Prototype(ent) is not { } proto)
                return;

            var config = new PolymorphConfiguration
            {
                Entity = proto,
                TransferDamage = true,
                TransferName = true,
                Forced = true,
                RevertOnCrit = false,
                RevertOnDeath = false,
                RevertOnEat = false,
                AllowRepeatedMorphs = true,
            };

            _polymorph.PolymorphEntity(ent, config);
            return;
        }

        if (ent.Comp.OldEyeColor is { } eyeColor)
            _humanoid.SetEyeColor(ent, eyeColor);
        if (ent.Comp.OldSkinColor is { } skinColor)
            _humanoid.SetSkinColor(ent, skinColor);

        var species = _proto.Index(humanoid.Species);
        var prototype = _proto.Index(species.Prototype);

        var comps = prototype.Components
            .IntersectBy(_proto.Index(ComponentsToRemoveOnGhoulify).Components.Keys, x => x.Key)
            .ToDictionary();

        EntityManager.AddComponents(ent, new ComponentRegistry(comps));

        var name = Factory.GetComponentName<MobThresholdsComponent>();
        if (prototype.Components.TryGetComponent(name, out var thresholds))
        {
            var component = (Component) Factory.GetComponent(name);
            var temp = (object) component;
            _seriMan.CopyTo(thresholds, ref temp);
            AddComp(ent, (Component) temp!, true);
        }

        if (TryComp(ent, out CollectiveMindComponent? collective))
            collective.Channels.Remove(SharedHereticAbilitySystem.MansusLinkMind);

        if (TryComp(ent, out NpcFactionMemberComponent? fact))
        {
            _faction.ClearFactions((ent, fact));
            _faction.AddFactions((ent.Owner, fact), ent.Comp.OldFactions);
        }

        if (_mind.TryGetMind(ent, out var mindId, out var mind))
            _role.MindRemoveRole<GhoulComponent>((mindId, mind));

        if (TryComp(ent, out HereticMinionComponent? minion))
        {
            if (Exists(minion.BoundHeretic) &&
                _heretic.TryGetHereticComponent(minion.BoundHeretic.Value, out var heretic, out var masterMind))
            {
                heretic.Minions.Remove(ent);
                if (TryComp(masterMind, out FleshHereticMindComponent? fleshMind))
                {
                    fleshMind.Ghouls.Remove(ent);
                    Dirty<HereticComponent, FleshHereticMindComponent>((masterMind, heretic, fleshMind));
                }
                else
                    Dirty(masterMind, heretic);
            }

            if (Exists(minion.CreationRitual) &&
                TryComp(minion.CreationRitual.Value, out Shared.Heretic.Rituals.HereticRitualComponent? ritual))
            {
                ritual.LimitedOutput.Remove(ent);
                Dirty(minion.CreationRitual.Value, ritual);
            }
        }

        if (TryComp(ent, out HolyFlammableComponent? holyFlam))
            _holyFlam.HolyExtinguish(ent, holyFlam);

        EntityManager.RemoveComponents(ent, _proto.Index(ComponentsToRemoveOnUnGhoulify).Components);
    }

    public void GhoulifyEntity(Entity<GhoulComponent> ent)
    {
        EntityManager.RemoveComponents(ent, _proto.Index(ComponentsToRemoveOnGhoulify).Components);

        EnsureComp<WeakToHolyComponent>(ent);
        var ev = new UnholyStatusChangedEvent(ent, ent, true);
        RaiseLocalEvent(ent, ref ev);

        EnsureComp<CombatModeComponent>(ent);

        EnsureComp<CollectiveMindComponent>(ent).Channels.Add(SharedHereticAbilitySystem.MansusLinkMind);

        if (TryComp(ent.Owner, out NpcFactionMemberComponent? fact))
        {
            ent.Comp.OldFactions = fact.Factions.ToHashSet();

            _faction.ClearFactions((ent.Owner, fact));
            _faction.AddFaction((ent.Owner, fact), HereticSystem.HereticFactionId);
        }

        var hasMind = _mind.TryGetMind(ent, out var mindId, out var mind);
        if (hasMind)
        {
            _mind.UnVisit(mindId, mind);
            if (!_role.MindHasRole<GhoulRoleComponent>(mindId))
            {
                SendBriefing(ent.Owner);
                _role.MindAddRole(mindId, GhoulRole, mind);
            }
        }
        else
        {
            var htn = EnsureComp<HTNComponent>(ent);
            htn.RootTask = new HTNCompoundTask { Task = Compound };
            _htn.Replan(htn);

            if (TryComp(ent.Owner, out HereticMinionComponent? minion) && minion.BoundHeretic is { } heretic)
                SetBoundHeretic((ent.Owner, minion), heretic, null, false);
        }

        _rejuvenate.PerformRejuvenate(ent);

        if (ent.Comp.ChangeHumanoidProfile && HasComp<HumanoidProfileComponent>(ent))
        {
            var organs = _humanoid.GetOrgansData(ent);
            ent.Comp.OldSkinColor = _humanoid.GetSkinColor(organs);
            ent.Comp.OldEyeColor = _humanoid.GetEyeColor(organs);

            var grey = Color.FromHex("#505050");
            _humanoid.SetEyeColor(ent, grey);
            _humanoid.SetSkinColor(ent, grey, grey);
        }

        if (ent.Comp.DeathBehavior == GhoulDeathBehavior.Deconvert)
            MakeOrgansFragile(ent);

        if (TryComp<MobThresholdsComponent>(ent, out var th))
        {
            _threshold.SetMobStateThreshold(ent, ent.Comp.TotalHealth, MobState.Dead, th);
            _threshold.SetMobStateThreshold(ent, ent.Comp.TotalHealth * 0.99f, MobState.Critical, th);
        }

        _mind.MakeSentient(ent);

        if (!hasMind)
        {
            var ghostRole = EnsureComp<GhostRoleComponent>(ent);
            ghostRole.RoleName = Loc.GetString(ent.Comp.GhostRoleName);
            ghostRole.RoleDescription = Loc.GetString(ent.Comp.GhostRoleDesc);
            ghostRole.RoleRules = Loc.GetString(ent.Comp.GhostRoleRules);
            ghostRole.MindRoles = [GhoulRole];
        }

        if (!HasComp<GhostRoleMobSpawnerComponent>(ent) && !hasMind)
            EnsureComp<GhostTakeoverAvailableComponent>(ent);

        if (TryComp(ent, out FleshMimickedComponent? mimicked))
        {
            foreach (var mimic in mimicked.FleshMimics)
            {
                if (!Exists(mimic))
                    continue;

                _faction.DeAggroEntity(mimic, ent);
            }

            RemCompDeferred(ent, mimicked);
        }

        GiveGhoulWeapon(ent);
    }

    /// <summary>
    /// Required to prevent heretic from farming organs from ghouls
    /// </summary>
    private void MakeOrgansFragile(EntityUid uid)
    {
        foreach (var organ in _body.GetOrgans(uid))
        {
            // Don't curse brain and torso
            if (_brainQuery.HasComp(organ) || _woundableQuery.TryComp(organ, out var woundable) &&
                woundable.RootWoundable == organ.Owner)
                continue;

            EnsureComp<FragileOrganComponent>(organ);
        }
    }

    private void SendBriefing(Entity<HereticMinionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var brief = Loc.GetString("heretic-ghoul-greeting-noname");
        var master = ent.Comp.BoundHeretic;

        if (Exists(master))
            brief = Loc.GetString("heretic-ghoul-greeting", ("ent", Identity.Entity(master.Value, EntityManager)));

        var sound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/Ambience/Antag/Heretic/heretic_gain.ogg");
        _antag.SendBriefing(ent, brief, Color.MediumPurple, sound);
    }

    private void OnGhoulInit(Entity<GhoulComponent> ent, ref MapInitEvent args)
    {
        GhoulifyEntity(ent);
    }

    private void OnShutdown(Entity<GhoulComponent> ent, ref ComponentShutdown args)
    {
        DestroyGhoulWeapon(ent);

        if (TerminatingOrDeleted(ent))
            return;

        var ev = new UnholyStatusChangedEvent(ent, ent, false);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnTakeGhostRole(Entity<HereticMinionComponent> ent, ref TakeGhostRoleEvent args)
    {
        SendBriefing(ent.AsNullable());
    }

    private void OnTryAttack(Entity<HereticMinionComponent> ent, ref AttackAttemptEvent args)
    {
        if (args.Target == null)
            return;

        if (args.Target == ent.Comp.BoundHeretic || HasComp<ShadowCloakEntityComponent>(args.Target.Value) &&
            Transform(args.Target.Value).ParentUid == ent.Comp.BoundHeretic)
            args.Cancel();
    }

    private void OnExamine(Entity<GhoulComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExamineMessage == null)
            return;

        args.PushMarkup(Loc.GetString(ent.Comp.ExamineMessage));
    }

    private void GiveGhoulWeapon(Entity<GhoulComponent> ent)
    {
        if (!ent.Comp.GiveBlade || !TryComp(ent, out HandsComponent? hands) || Exists(ent.Comp.BoundWeapon))
            return;

        var blade = Spawn(ent.Comp.BladeProto, Transform(ent).Coordinates);
        EnsureComp<GhoulWeaponComponent>(blade);
        ent.Comp.BoundWeapon = blade;

        if (!_hands.TryPickup(ent, blade, animate: false, handsComp: hands) &&
            _inventory.TryGetSlotEntity(ent, "back", out var slotEnt) &&
            _storage.CanInsert(slotEnt.Value, blade, out _))
            _storage.Insert(slotEnt.Value, blade, out _, out _, playSound: false);
    }

    private void DestroyGhoulWeapon(Entity<GhoulComponent> ent)
    {
        if (ent.Comp.BoundWeapon == null || TerminatingOrDeleted(ent.Comp.BoundWeapon.Value))
            return;

        _audio.PlayPvs(ent.Comp.BladeDeleteSound, Transform(ent.Comp.BoundWeapon.Value).Coordinates);
        QueueDel(ent.Comp.BoundWeapon.Value);
    }

    private void OnMobStateChange(Entity<GhoulComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
        {
            if (args.NewMobState == MobState.Alive)
                GiveGhoulWeapon(ent);
            return;
        }

        DestroyGhoulWeapon(ent);

        switch (ent.Comp.DeathBehavior)
        {
            case GhoulDeathBehavior.NoGib:
                return;
            case GhoulDeathBehavior.Deconvert:
                UnGhoulifyEntity(ent);
                return;
        }

        if (ent.Comp.SpawnOnDeathPrototype != null)
            Spawn(ent.Comp.SpawnOnDeathPrototype.Value, Transform(ent).Coordinates);

        if (!HasComp<BodyComponent>(ent))
            return;

        foreach (var giblet in _gibbing.Gib(ent, ent.Comp.DeathBehavior == GhoulDeathBehavior.GibOrgans))
        {
            RemComp<NymphComponent>(giblet); // no reforming chuddy
        }
    }
}

// SPDX-FileCopyrightText: 2024 Errant <35878406+Errant-4@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 whateverusername0 <whateveremail>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 JohnOakman <sremy2012@hotmail.fr>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 TheBorzoiMustConsume <197824988+TheBorzoiMustConsume@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 github-actions <github-actions@github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Antag;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Hands.Components;
using Content.Server.Hands.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Shitcode.Roles;
using Content.Shared.Administration.Systems;
using Content.Shared.CombatMode;
using Content.Shared.Examine;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Heretic;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Robust.Server.Audio;
using Content.Goobstation.Shared.Religion;
using Content.Goobstation.Shared.Religion.Nullrod;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Wounds;
using Content.Server.Heretic.Abilities;
using Content.Server.Heretic.EntitySystems;
using Content.Server.Jittering;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Roles;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared._Starlight.CollectiveMind;
using Content.Shared.Body;
using Content.Shared.Coordinates;
using Content.Shared.Roles;
using Content.Shared.Species.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Polymorph;
using Content.Server.Polymorph.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared._Shitcode.Heretic.Rituals;
using Content.Shared.Gibbing;
using Content.Shared.NPC.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.Roles.Components;
using Content.Trauma.Server.Chaplain;
using Content.Trauma.Shared.Chaplain.Components;
using Content.Trauma.Shared.Heretic.Systems;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed class GhoulSystem : SharedGhoulSystem
{
    private static readonly ProtoId<HTNCompoundPrototype> Compound = "HereticSummonCompound";
    private static readonly EntProtoId<MindRoleComponent> GhoulRole = "MindRoleGhoul";

    private static readonly EntProtoId ComponentsToRemoveOnGhoulify = "ComponentsToRemoveOnGhoulify";
    private static readonly EntProtoId ComponentsToRemoveOnUnGhoulify = "ComponentsToRemoveOnUnGhoulify";

    private readonly string[] _ignoredComponentsOnTransfer = ["Transform", "MetaData"];

    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly JitteringSystem _jitter = default!;
    [Dependency] private readonly StutteringSystem _stutter = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly StorageSystem _storage = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;
    [Dependency] private readonly HolyFlammableSystem _holyFlam = default!;
    [Dependency] private readonly HumanoidProfileSystem _humanoid = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(HolyFlammableSystem));
        SubscribeLocalEvent<GhoulComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GhoulComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<GhoulComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<GhoulComponent, MobStateChangedEvent>(OnMobStateChange);
        SubscribeLocalEvent<GhoulComponent, SetGhoulBoundHereticEvent>(OnBound);
        SubscribeLocalEvent<GhoulComponent, UserShouldTakeHolyEvent>(OnShouldTakeHoly);

        SubscribeLocalEvent<GhoulDeconvertComponent, ComponentStartup>(OnDeconvertStartup);
        SubscribeLocalEvent<GhoulDeconvertComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<GhoulDeconvertComponent, DamageUnholyEvent>(OnDamageUnholy,
            after: [typeof(WeakToHolySystem)]);

        SubscribeLocalEvent<GhoulRoleComponent, GetBriefingEvent>(OnGetBriefing);

        SubscribeLocalEvent<GhoulWeaponComponent, ExaminedEvent>(OnWeaponExamine);

        SubscribeLocalEvent<VoicelessDeadComponent, MapInitEvent>(OnVoicelessDeadInit);
        SubscribeLocalEvent<VoicelessDeadComponent, ComponentShutdown>(OnVoicelessDeadShutdown);

        SubscribeLocalEvent<HereticMinionComponent, AttackAttemptEvent>(OnTryAttack);
        SubscribeLocalEvent<HereticMinionComponent, TakeGhostRoleEvent>(OnTakeGhostRole);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GhoulDeconvertComponent, GhoulComponent>();
        while (query.MoveNext(out var uid, out var deconvert, out var ghoul))
        {
            deconvert.Delay -= frameTime;

            if (deconvert.Delay > 0f)
                continue;

            UnGhoulifyEntity((uid, ghoul));
        }
    }

    private void OnShouldTakeHoly(Entity<GhoulComponent> ent, ref UserShouldTakeHolyEvent args)
    {
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.WeakToHoly = true;
        args.ShouldTakeHoly = true;
    }

    private void OnDamageUnholy(Entity<GhoulDeconvertComponent> ent, ref DamageUnholyEvent args)
    {
        args.ShouldTakeHoly = false;
    }

    private void OnRejuvenate(Entity<GhoulDeconvertComponent> ent, ref RejuvenateEvent args)
    {
        RemCompDeferred(ent, ent.Comp);
    }

    private void OnDeconvertStartup(Entity<GhoulDeconvertComponent> ent, ref ComponentStartup args)
    {
        var time = TimeSpan.FromSeconds(ent.Comp.Delay);
        _jitter.DoJitter(ent, time, true);
        _stutter.DoStutter(ent, time, true);
    }

    private void OnVoicelessDeadShutdown(Entity<VoicelessDeadComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        ProcessVoicelessDeadBody(ent, true);
    }

    private void OnVoicelessDeadInit(Entity<VoicelessDeadComponent> ent, ref MapInitEvent args)
    {
        ProcessVoicelessDeadBody(ent, false);
    }

    private void OnBound(Entity<GhoulComponent> ent, ref SetGhoulBoundHereticEvent args)
    {
        SetBoundHeretic(ent.Owner, args.Heretic, args.Ritual);
    }

    private void ProcessVoicelessDeadBody(EntityUid uid, bool makeRemovable)
    {
        var woundableQuery = GetEntityQuery<WoundableComponent>();
        foreach (var organ in _body.GetOrgans(uid))
        {
            if (woundableQuery.TryComp(organ, out var woundable) && woundable.RootWoundable == organ.Owner)
                continue;

            if (makeRemovable)
                RemCompDeferred<UnremoveableOrganComponent>(organ);
            else
                EnsureComp<UnremoveableOrganComponent>(organ);
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
        if (!ent.Comp.CanDeconvert)
            return;

        // You can't have non-humanoid deconvertible ghouls normally, but this is here just in case
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

        if (ent.Comp.OldEyeColor is {} eyeColor)
            _humanoid.SetEyeColor(ent, eyeColor);
        if (ent.Comp.OldSkinColor is {} skinColor)
            _humanoid.SetSkinColor(ent, skinColor);

        var species = _proto.Index(humanoid.Species);
        var prototype = _proto.Index(species.Prototype);

        var comps = prototype.Components
            .IntersectBy(_proto.Index(ComponentsToRemoveOnGhoulify).Components.Keys.Except(_ignoredComponentsOnTransfer),
                x => x.Key)
            .ToDictionary();

        EntityManager.AddComponents(ent, new ComponentRegistry(comps));
        if (prototype.Components.TryGetComponent(Factory.GetComponentName<MobThresholdsComponent>(),
                out var thresholds))
            AddComp(ent, thresholds, true);

        if (TryComp(ent, out CollectiveMindComponent? collective))
            collective.Channels.Remove(HereticAbilitySystem.MansusLinkMind);

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
                Dirty(masterMind, heretic);
            }

            if (Exists(minion.CreationRitual) &&
                TryComp(minion.CreationRitual.Value, out HereticRitualComponent? ritual))
            {
                ritual.LimitedOutput.Remove(ent);
                Dirty(minion.CreationRitual.Value, ritual);
            }
        }

        if (TryComp(ent, out HolyFlammableComponent? holyFlam))
            _holyFlam.HolyExtinguish(ent, holyFlam);

        var comps2 = _proto.Index(ComponentsToRemoveOnUnGhoulify)
            .Components.ExceptBy(_ignoredComponentsOnTransfer, x => x.Key)
            .ToDictionary();
        EntityManager.RemoveComponents(ent, new ComponentRegistry(comps2));
    }

    public void GhoulifyEntity(Entity<GhoulComponent> ent)
    {
        var comps = _proto.Index(ComponentsToRemoveOnGhoulify)
            .Components.ExceptBy(_ignoredComponentsOnTransfer, x => x.Key)
            .ToDictionary();
        EntityManager.RemoveComponents(ent, new ComponentRegistry(comps));

        EnsureComp<WeakToHolyComponent>(ent);
        var ev = new UnholyStatusChangedEvent(ent, ent, true);
        RaiseLocalEvent(ent, ref ev);

        EnsureComp<CombatModeComponent>(ent);

        EnsureComp<CollectiveMindComponent>(ent).Channels.Add(HereticAbilitySystem.MansusLinkMind);

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

        if (HasComp<HumanoidProfileComponent>(ent))
        {
            var organs = _humanoid.GetOrgansData(ent);
            ent.Comp.OldSkinColor = _humanoid.GetSkinColor(organs);
            ent.Comp.OldEyeColor = _humanoid.GetEyeColor(organs);

            var grey = Color.FromHex("#505050");
            _humanoid.SetEyeColor(ent, grey);
            _humanoid.SetSkinColor(ent, grey, grey);
        }

        _rejuvenate.PerformRejuvenate(ent);

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

    private void OnStartup(Entity<GhoulComponent> ent, ref ComponentStartup args)
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

        if (ent.Comp.DeathBehavior == GhoulDeathBehavior.NoGib)
            return;

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

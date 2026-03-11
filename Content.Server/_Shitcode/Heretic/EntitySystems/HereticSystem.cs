// SPDX-FileCopyrightText: 2024 Errant <35878406+Errant-4@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 whateverusername0 <whateveremail>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GMWQ <garethquaile@gmail.com>
// SPDX-FileCopyrightText: 2025 Gareth Quaile <garethquaile@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 TheBorzoiMustConsume <197824988+TheBorzoiMustConsume@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2025 gonz0 <105350621+doktor-gonz0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gonz0 < 105350621+doktor-gonz0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Religion;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Eye;
using Content.Shared.Heretic;
using Content.Shared.Mind;
using Content.Shared.Store.Components;
using Content.Shared.Heretic.Prototypes;
using Content.Server.Chat.Systems;
using Robust.Shared.Audio;
using Content.Server.Heretic.Components;
using Content.Server.Antag;
using Robust.Shared.Random;
using System.Linq;
using Content.Goobstation.Shared.ManifestListings;
using Content.Goobstation.Shared.Religion.Nullrod;
using Content.Server._Goobstation.Objectives.Components;
using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules;
using Content.Shared.Humanoid;
using Content.Server.Revolutionary.Components;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Server.Polymorph.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared._Shitcode.Heretic.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Server.Hands.Systems;
using Content.Shared._Shitcode.Heretic.Rituals;
using Content.Shared.Jaunt;
using Content.Shared.Mobs;
using Content.Shared.StatusEffectNew;
using Content.Shared.Store;
using Content.Shared.Tag;
using Robust.Server.GameStates;
using Robust.Shared.Utility;

namespace Content.Server.Heretic.EntitySystems;

public sealed class HereticSystem : SharedHereticSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PvsOverrideSystem _override = default!;
    [Dependency] private readonly HereticRuleSystem _rule = default!;

    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;

    private float _timer;
    private const float PassivePointCooldown = 20f * 60f;

    private const int HereticVisFlags = (int) VisibilityFlags.EldritchInfluence;

    public static readonly ProtoId<NpcFactionPrototype> HereticFactionId = "Heretic";

    public static readonly ProtoId<NpcFactionPrototype> NanotrasenFactionId = "NanoTrasen";

    public static readonly ProtoId<TagPrototype> AscensionRitualTag = "RitualAscension";

    public static readonly ProtoId<TagPrototype> FeastOfOwlsRitualTag = "RitualFeastOfOwls";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticComponent, ComponentStartup>(OnCompStartup);
        SubscribeLocalEvent<HereticComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HereticComponent, EventHereticUpdateTargets>(OnUpdateTargets);
        SubscribeLocalEvent<HereticComponent, EventHereticRerollTargets>(OnRerollTargets);
        SubscribeLocalEvent<HereticComponent, EventHereticAscension>(OnAscension);
        SubscribeLocalEvent<HereticComponent, ListingPurchasedEvent>(OnPurchase);

        SubscribeLocalEvent<HereticComponent, MindGotRemovedEvent>(OnMindRemoved);
        SubscribeLocalEvent<HereticComponent, MindGotAddedEvent>(OnMindAdded);

        SubscribeLocalEvent<GetVisMaskEvent>(OnGetVisMask);
        SubscribeLocalEvent<HereticStartupEvent>(OnHereticStartup);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRestart);
        SubscribeLocalEvent<UserShouldTakeHolyEvent>(OnShouldTakeHoly);
        SubscribeLocalEvent<MobStateChangedEvent>(OnStateChanged);

        SubscribeLocalEvent<HideHereticAuraStatusEffectComponent, StatusEffectAppliedEvent>(OnApply);
        SubscribeLocalEvent<HideHereticAuraStatusEffectComponent, StatusEffectRemovedEvent>(OnRemove);
    }

    private void OnStateChanged(MobStateChangedEvent args)
    {
        if (!TryGetHereticComponent(args.Target, out var heretic, out var mind))
            return;

        var newActive = args.NewMobState == MobState.Dead;
        if (heretic.IsActive == newActive)
            return;

        heretic.IsActive = newActive;

        var ev = new HereticStateChangedEvent(mind, !newActive, false);
        foreach (var minion in heretic.Minions)
        {
            RaiseLocalEvent(minion, ref ev);
        }
    }

    private void OnRemove(Entity<HideHereticAuraStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        UpdateHereticAura(args.Target);
    }

    private void OnApply(Entity<HideHereticAuraStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        RemCompDeferred<HereticAuraComponent>(args.Target);
    }

    private void OnMindAdded(Entity<HereticComponent> ent, ref MindGotAddedEvent args)
    {
        ent.Comp.MansusGraspAction = EntityUid.Invalid;

        if (TerminatingOrDeleted(args.Container))
            return;

        if (!TryComp(args.Container, out MobStateComponent? mobState))
        {
            if (!ent.Comp.IsActive)
                return;
            // Don't kill stargazer if we got temporarily polymorphed
            var temporary = TryComp(args.Container, out PolymorphedEntityComponent? p) &&
                            (!p.Configuration.Forced || p.Configuration.Duration != null) ||
                            HasComp<JauntComponent>(args.Container);

            ent.Comp.IsActive = false;
            var ev = new HereticStateChangedEvent(ent, true, temporary);
            foreach (var minion in ent.Comp.Minions)
            {
                RaiseLocalEvent(minion, ref ev);
            }

            return;
        }

        var newActive = mobState.CurrentState != MobState.Dead;
        if (newActive != ent.Comp.IsActive)
        {
            var ev = new HereticStateChangedEvent(ent, !newActive, false);
            foreach (var minion in ent.Comp.Minions)
            {
                RaiseLocalEvent(minion, ref ev);
            }
            ent.Comp.IsActive = newActive;
        }

        SetMinionsMaster(ent, args.Container);
        RaiseKnowledgeEvents(ent, args.Container, false);

        if (!ent.Comp.Ascended)
            return;

        var ev2 = new UnholyStatusChangedEvent(args.Container, args.Container, true);
        RaiseLocalEvent(args.Container, ref ev2);
    }

    private void OnMindRemoved(Entity<HereticComponent> ent, ref MindGotRemovedEvent args)
    {
        ent.Comp.MansusGraspAction = EntityUid.Invalid;

        if (TerminatingOrDeleted(args.Container) || !HasComp<MobStateComponent>(args.Container))
            return;

        SetMinionsMaster(ent, null);
        RaiseKnowledgeEvents(ent, args.Container, true);
    }

    private void SetMinionsMaster(Entity<HereticComponent> ent, EntityUid? newMaster)
    {
        ent.Comp.Minions = ent.Comp.Minions.Where(Exists).ToHashSet();
        var mobQuery = GetEntityQuery<MobStateComponent>();
        foreach (var uid in ent.Comp.Minions)
        {
            if (!mobQuery.HasComp(uid))
                continue;
            var minion = EnsureComp<HereticMinionComponent>(uid);
            minion.BoundHeretic = newMaster;
            Dirty(uid, minion);
        }
    }

    private void RaiseKnowledgeEvents(Entity<HereticComponent> mind, EntityUid body, bool negative)
    {
        foreach (var ev in mind.Comp.KnowledgeEvents)
        {
            RaiseKnowledgeEvent(body, ev, negative);
        }
    }

    public override void RaiseKnowledgeEvent(EntityUid uid, HereticKnowledgeEvent ev, bool negative)
    {
        if (negative)
            EntityManager.RemoveComponents(uid, ev.AddedComponents);
        else
            EntityManager.AddComponents(uid, ev.AddedComponents);
        ev.Negative = negative;
        ev.Heretic = uid;
        RaiseLocalEvent(uid, (object) ev, true);
    }

    protected override void SpawnRituals(HereticComponent heretic,
        List<EntProtoId<HereticRitualComponent>> rituals,
        ICommonSession session)
    {
        base.SpawnRituals(heretic, rituals, session);

        foreach (var ritual in rituals)
        {
            var ritUid = Spawn(ritual);
            _override.AddSessionOverride(ritUid, session);
            heretic.Rituals.Add(ritUid);
        }
    }

    private void OnHereticStartup(HereticStartupEvent ev)
    {
        foreach (var item in _hands.EnumerateHeld(ev.Heretic))
        {
            if (HasComp<MansusGraspComponent>(item))
                QueueDel(item);
        }

        if (ev.Negative)
            _npcFaction.RemoveFaction(ev.Heretic, HereticFactionId);
        else
        {
            _npcFaction.RemoveFaction(ev.Heretic, NanotrasenFactionId, false);
            _npcFaction.AddFaction(ev.Heretic, HereticFactionId);
        }

        UpdateHereticAura(ev.Heretic);

        if (!TryComp<EyeComponent>(ev.Heretic, out var eye))
            return;

        var mask = ev.Negative ? eye.VisibilityMask & ~HereticVisFlags : eye.VisibilityMask | HereticVisFlags;
        _eye.SetVisibilityMask(ev.Heretic, mask, eye);
    }

    private void OnRestart(RoundRestartCleanupEvent ev)
    {
        _timer = 0f;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _timer += frameTime;

        if (_timer < PassivePointCooldown)
            return;

        _timer = 0f;

        var query = EntityQueryEnumerator<HereticComponent, StoreComponent, MindComponent>();
        while (query.MoveNext(out var uid, out var heretic, out var store, out var mind))
        {
            // passive point gain every 20 minutes
            UpdateMindKnowledge((uid, heretic, store, mind), null, 1f);
        }
    }

    public override void UpdateMindKnowledge(Entity<HereticComponent, StoreComponent, MindComponent> ent,
        EntityUid? user,
        float amount,
        bool showText = true,
        bool playSound = true)
    {
        base.UpdateMindKnowledge(ent, user, amount, showText, playSound);

        var (mindId, heretic, store, mind) = ent;
        var uid = user ?? mind.OwnedEntity;

        _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { "KnowledgePoint", amount } }, mindId, store);
        _store.UpdateUserInterface(uid, mindId, store);

        if (_mind.TryGetObjectiveComp<HereticKnowledgeConditionComponent>(mindId, out var objective, mind))
            objective.Researched += amount;

        if (!showText && !playSound)
            return;

        if (!PlayerMan.TryGetSessionById(mind.UserId, out var session))
            return;

        if (showText)
        {
            var baseMessage = heretic.InfluenceGainBaseMessage;
            var message = Loc.GetString(_rand.Pick(heretic.InfluenceGainMessages));
            var size = heretic.InfluenceGainTextFontSize;
            var loc = Loc.GetString(baseMessage, ("size", size), ("text", message));
            SharedChatSystem.UpdateFontSize(size, ref message, ref loc);
            _chatMan.ChatMessageToOne(ChatChannel.Server,
                message,
                loc,
                default,
                false,
                session.Channel,
                canCoalesce: false);
        }

        if (playSound)
            _audio.PlayGlobal(heretic.InfluenceGainSound, session);

        var couldBreak = heretic.CanBreakBlade;
        var hadAura = heretic.ShouldShowAura;
        heretic.KnowledgeTracker += amount;
        var canBreak = heretic.CanBreakBlade;
        var showAura = heretic.ShouldShowAura;

        if (!canBreak && couldBreak)
        {
            var msg = Loc.GetString(heretic.BreakBladeAbilityLostMessage);
            _chatMan.ChatMessageToOne(ChatChannel.Server,
                msg,
                msg,
                default,
                false,
                session.Channel,
                Color.Red);
        }

        if (!hadAura && showAura)
        {
            if (uid != null)
                Status.TryUpdateStatusEffectDuration(uid.Value, heretic.HideAuraStatusEffect, heretic.AuraDelayTime);

            var msg = Loc.GetString(heretic.AuraVisibleMessage);
            _chatMan.ChatMessageToOne(ChatChannel.Server,
                msg,
                msg,
                default,
                false,
                session.Channel,
                Color.Red);
        }

        Dirty(mindId, heretic);
    }

    private void OnCompStartup(Entity<HereticComponent> ent, ref ComponentStartup args)
    {
        foreach (var k in ent.Comp.BaseKnowledge)
        {
            TryAddKnowledge((ent, null, ent), k);
        }

        RaiseLocalEvent(ent, new EventHereticRerollTargets());
        UpdateHereticCostModifiers(ent.AsNullable());
    }

    private void OnShutdown(Entity<HereticComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp(ent, out MindComponent? mind) && mind.CurrentEntity is { } body && !TerminatingOrDeleted(body))
        {
            SetMinionsMaster(ent, null);
            RaiseKnowledgeEvents(ent, body, true);
        }

        if (TerminatingOrDeleted(ent) || !TryComp(ent, out ActionsContainerComponent? container))
            return;

        foreach (var action in container.Container.ContainedEntities.ToList())
        {
            if (HasComp<HereticActionComponent>(action))
                _actionContainer.RemoveAction(action);
        }

        foreach (var ritual in ent.Comp.Rituals)
        {
            if (!TerminatingOrDeleted(ritual))
                QueueDel(ritual);
        }
    }

    private void OnGetVisMask(ref GetVisMaskEvent args)
    {
        if (!TryGetHereticComponent(args.Entity, out _, out _))
            return;

        args.VisibilityMask |= HereticVisFlags;
    }

    private void OnShouldTakeHoly(ref UserShouldTakeHolyEvent ev)
    {
        if (!TryGetHereticComponent(ev.Target, out var heretic, out _))
            return;

        ev.ShouldTakeHoly |= heretic.Ascended;
        ev.WeakToHoly = true;
    }

    private void OnUpdateTargets(Entity<HereticComponent> ent, ref EventHereticUpdateTargets args)
    {
        ent.Comp.SacrificeTargets = ent.Comp.SacrificeTargets
            .Where(target => TryGetEntity(target.Entity, out var tent) && Exists(tent) &&
                             !EntityManager.IsQueuedForDeletion(tent.Value))
            .ToList();
        Dirty(ent); // update client
    }

    private void OnRerollTargets(Entity<HereticComponent> ent, ref EventHereticRerollTargets args)
    {
        // welcome to my linq smorgasbord of doom
        // have fun figuring that out

        var targets = _antag.GetAliveConnectedPlayers(PlayerMan.Sessions)
            .Where(IsSessionValid)
            .Select(x => x.AttachedEntity!.Value)
            .ToList();

        var pickedTargets = new List<EntityUid>();

        var predicates = new List<Func<EntityUid, bool>>();

        // pick one command staff
        predicates.Add(HasComp<CommandStaffComponent>);
        // pick one security staff
        predicates.Add(HasComp<SecurityStaffComponent>);

        // add more predicates here

        foreach (var predicate in predicates)
        {
            var list = targets.Where(predicate).ToList();

            if (list.Count == 0)
                continue;

            // pick and take
            var picked = _rand.Pick(list);
            targets.Remove(picked);
            pickedTargets.Add(picked);
        }

        // add whatever more until satisfied
        for (var i = 0; i <= ent.Comp.MaxTargets - pickedTargets.Count; i++)
        {
            if (targets.Count > 0)
                pickedTargets.Add(_rand.PickAndTake(targets));
        }

        // leave only unique entityuids
        pickedTargets = pickedTargets.Distinct().ToList();

        ent.Comp.SacrificeTargets = pickedTargets.Select(GetData).OfType<SacrificeTargetData>().ToList();
        Dirty(ent); // update client

        return;

        bool IsSessionValid(ICommonSession session)
        {
            if (!HasComp<HumanoidProfileComponent>(session.AttachedEntity))
                return false;

            if (HasComp<GhoulComponent>(session.AttachedEntity.Value))
                return false;

            if (!_mind.TryGetMind(session.AttachedEntity.Value, out var mind, out _) ||
                mind == ent.Owner || !_job.MindTryGetJobId(mind, out _))
                return false;

            return !HasComp<HereticComponent>(mind);
        }
    }

    private SacrificeTargetData? GetData(EntityUid uid)
    {
        if (!TryComp(uid, out HumanoidProfileComponent? humanoid))
            return null;

        if (!_mind.TryGetMind(uid, out var mind, out _) || !_job.MindTryGetJobId(mind, out var jobId) || jobId == null)
            return null;

        /* TODO NUBODY: use api if it gets made
        var appearance = new HumanoidCharacterAppearance(hair.Item1,
            hair.Item2,
            facialHair.Item1,
            facialHair.Item2,
            humanoid.EyeColor,
            humanoid.SkinColor,
            humanoid.MarkingSet.GetForwardEnumerator().ToList());
        */

        var profile = new HumanoidCharacterProfile().WithGender(humanoid.Gender)
            .WithSex(humanoid.Sex)
            .WithSpecies(humanoid.Species)
            .WithName(MetaData(uid).EntityName)
            .WithAge(humanoid.Age);
            //.WithCharacterAppearance(appearance);

        var netEntity = GetNetEntity(uid);

        return new SacrificeTargetData { Entity = netEntity, Profile = profile, Job = jobId.Value };
    }

    // notify the crew of how good the person is and play the cool sound :godo:
    private void OnAscension(Entity<HereticComponent> ent, ref EventHereticAscension args)
    {
        if (!TryComp(ent, out MindComponent? mind) || mind.CurrentEntity is not { } uid)
            return;

        // you've already ascended, man.
        if (ent.Comp.Ascended || !ent.Comp.CanAscend)
            return;

        ent.Comp.Ascended = true;
        RemoveRituals(ent.AsNullable(), [AscensionRitualTag, FeastOfOwlsRitualTag]);
        ent.Comp.ChosenRitual = null;
        Dirty(ent);

        UpdateHereticAura(uid);

        // how???
        if (ent.Comp.CurrentPath == null)
            return;

        if (TryComp(ent, out ActionsContainerComponent? container))
        {
            foreach (var action in container.Container.ContainedEntities)
            {
                if (TryComp(action, out ChangeUseDelayOnAscensionComponent? changeUseDelay) &&
                    (changeUseDelay.RequiredPath == null || changeUseDelay.RequiredPath == ent.Comp.CurrentPath))
                    _actions.SetUseDelay(action, changeUseDelay.NewUseDelay);
            }
        }

        var pathLoc = ent.Comp.CurrentPath.ToLower();
        var ascendSound =
            new SoundPathSpecifier($"/Audio/_Goobstation/Heretic/Ambience/Antag/Heretic/ascend_{pathLoc}.ogg");
        _chat.DispatchGlobalAnnouncement(Loc.GetString($"heretic-ascension-{pathLoc}"),
            Name(uid),
            true,
            ascendSound,
            Color.Pink);
    }


    private void OnPurchase(Entity<HereticComponent> ent, ref ListingPurchasedEvent args)
    {
        if (args.Data.Categories.FirstOrNull() is not { } cat)
            return;

        if (!ent.Comp.SideKnowledgeDrafts.TryGetValue(cat, out var amount))
            return;

        var listings = _store.GetAvailableListings(args.User, ent, Comp<StoreComponent>(ent));
        foreach (var listing in listings)
        {
            if (listing == args.Data ||
                !listing.Categories.Contains(cat) ||
                !listing.CostModifiersBySourceId.ContainsKey(cat))
                continue;

            listing.RemoveCostModifier(cat);
        }

        var newAmount = Math.Max(amount - 1, 0);
        ent.Comp.SideKnowledgeDrafts[cat] = newAmount;
        if (newAmount > 0)
            UpdateHereticCostModifiers(ent.AsNullable(), cat, args.Data);
    }

    public override void UpdateHereticCostModifiers(Entity<HereticComponent?> ent,
        ProtoId<StoreCategoryPrototype>? category = null,
        ListingDataWithCostModifiers? except = null)
    {
        base.UpdateHereticCostModifiers(ent, category, except);

        if (!Resolve(ent, ref ent.Comp))
            return;

        var store = CompOrNull<StoreComponent>(ent) ?? _rule.InitializeStore(ent);

        var allListings = _store.GetAvailableListings(ent, ent, store).ToList();

        if (except is { } e)
            allListings.Remove(e);

        // Order listings by category
        var listings = allListings
            .Where(x => x.Categories.FirstOrNull() is { } cat && (category == null || cat == category) &&
                        ent.Comp.SideKnowledgeDrafts.TryGetValue(cat, out var amount) && amount > 0)
            .DistinctBy(x => x.Categories.First())
            .ToDictionary(x => x.Categories.First(),
                x => allListings.Where(y => y.Categories.Intersect(x.Categories).Any()).ToList());

        foreach (var (key, value) in listings)
        {
            if (value.Count == 0 || value.Any(x => x.CostModifiersBySourceId.ContainsKey(key)))
                continue;

            var amount = Math.Min(value.Count, ent.Comp.SideDraftChoiceAmount);
            for (var i = 0; i < amount; i++)
            {
                var listing = _rand.PickAndTake(value);
                listing.AddCostModifier(key, listing.Cost.ToDictionary(x => x.Key, _ => -FixedPoint2.New(1)));
            }
        }
    }
}

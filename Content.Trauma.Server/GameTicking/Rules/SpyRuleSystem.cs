using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Goobstation.Server.ManifestListings;
using Content.Goobstation.Shared.ManifestListings;
using Content.Medical.Shared.Body;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Server.Traitor.Uplink;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Store.Components;
using Content.Trauma.Server.GameTicking.Rules.Components;
using Content.Trauma.Server.Spy;
using Content.Trauma.Shared.Areas;
using Content.Trauma.Shared.Roles;
using Content.Trauma.Shared.Spy;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.GameTicking.Rules;

public sealed partial class SpyRuleSystem : GameRuleSystem<SpyRuleComponent>
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private UplinkSystem _uplink = default!;
    [Dependency] private SpyUplinkSystem _spyUplink = default!;
    [Dependency] private RoleSystem _role = default!;
    [Dependency] private AreaSystem _area = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private StoreSystem _store = default!;
    [Dependency] private JobSystem _job = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private BodySystem _body = default!;

    [Dependency] private EntityQuery<HumanoidProfileComponent> _humanoidQuery = default!;
    [Dependency] private EntityQuery<BrainComponent> _brainQuery = default!;
    [Dependency] private EntityQuery<HeartComponent> _heartQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrependObjectivesSummaryTextEvent>(OnPrepend, before: [typeof(ManifestListingsSystem)]);
    }


    protected override void ActiveTick(EntityUid uid,
        SpyRuleComponent component,
        GameRuleComponent gameRule,
        float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        var now = _timing.CurTime;

        if (component.NextRefresh > now)
            return;

        RefreshBounties(uid, component, now, component.RefreshTime);
    }

    protected override void Started(EntityUid uid,
        SpyRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        foreach (var grid in _station.GetAllStationGrids())
        {
            component.StationMaps.Add(Transform(grid).MapID);
        }

        GenerateLootPool((uid, component));
        RefreshBounties(uid, component, _timing.CurTime, component.FirstRefreshTime);
    }

    private void GenerateLootPool(Entity<SpyRuleComponent, StoreComponent?> ent)
    {
        var (uid, comp, store) = ent;

        if (!Resolve(uid, ref store))
            return;

        store.LastAvailableListings = _store.GetAvailableListings(uid, uid, store).ToHashSet();

        var tc = UplinkSystem.TelecrystalCurrencyPrototype;

        foreach (var listing in store.LastAvailableListings)
        {
            if (!listing.OriginalCost.TryGetValue(tc, out var cost) || listing.ProductEntity == null)
                continue;

            var difficulty = SpyBountyDifficulty.Easy;

            foreach (var (key, value) in comp.CostToDifficulty)
            {
                if (cost < key)
                    break;

                difficulty = value;
            }

            comp.LootPool.GetOrNew(difficulty)[listing.ID] = 1f;
        }

        foreach (var proto in ProtoMan.EnumeratePrototypes<SpyRewardPrototype>())
        {
            comp.LootPool.GetOrNew(proto.Difficulty)[proto.ID] = proto.Weight;
        }
    }

    private void RefreshBounties(EntityUid uid, SpyRuleComponent rule, TimeSpan curTime, TimeSpan refreshTime)
    {
        foreach (var bounty in rule.CurrentBounties)
        {
            if (bounty.Claimed)
                rule.ClaimedBounties.Add(bounty.BountyProto);
        }

        rule.CurrentBounties.Clear();
        rule.NextRefresh = curTime + refreshTime;

        if (rule.BountyPool is not { } pool || pool.Count < rule.NumBounties)
            GenerateBountyPool(rule);

        for (var i = 0; i < rule.NumBounties; i++)
        {
            if (!GetRandomBounty(uid, rule))
                break;
        }

        _spyUplink.RefreshUi(rule.NextRefresh, rule.CurrentBounties);
    }

    private void GenerateBountyPool(SpyRuleComponent rule)
    {
        rule.BountyPool = [];
        FillBountyPool(rule, rule.BountyPoolProto);

        // If we ran out of bounties reset claimed bounties and include them in selection
        if (rule.BountyPool.Count < rule.NumBounties)
        {
            rule.ClaimedBounties.Clear();
            rule.BountyPool.Clear();
            FillBountyPool(rule, rule.BountyPoolProto);
        }
    }

    private void FillBountyPool(SpyRuleComponent rule, ProtoId<WeightedRandomPrototype> random, bool recursion = true)
    {
        var index = ProtoMan.Index(random);
        foreach (var (key, value) in index.Weights)
        {
            if (ProtoMan.HasIndex<SpyBountyPrototype>(key))
            {
                if (!rule.UnavailableBounties.Contains(key) && !rule.ClaimedBounties.Contains(key))
                    rule.BountyPool![key] = value;
                continue;
            }

            if (!recursion)
            {
                Log.Error($"Expected {key} to be SpyBountyPrototype");
                continue;
            }

            if (!ProtoMan.HasIndex<WeightedRandomPrototype>(key))
            {
                Log.Error($"Expected {key} to be SpyBountyPrototype or WeightedRandomPrototype");
                continue;
            }

            FillBountyPool(rule, key, false);
        }
    }

    [SubscribeLocalEvent]
    private void OnGetBriefing(Entity<SpyRoleComponent> ent, ref GetBriefingEvent args)
    {
        args.Append(ent.Comp.Briefing);
    }

    [SubscribeLocalEvent]
    private void AfterEntitySelected(Entity<SpyRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        MakeSpy(args.EntityUid, ent);
    }

    public bool MakeSpy(EntityUid spy, Entity<SpyRuleComponent> rule)
    {
        if (!_mind.TryGetMind(spy, out var mindId, out _))
        {
            Log.Debug($"MakeSpy {ToPrettyString(spy)} - failed, no Mind found");
            return false;
        }

        var briefing = Loc.GetString("spy-role-briefing-short");

        if (rule.Comp.GiveUplink)
            briefing = RequestUplink(spy, mindId, briefing);

        if (_role.MindHasRole<SpyRoleComponent>(mindId, out var role))
        {
            role.Value.Comp2.Briefing = briefing;
            role.Value.Comp2.Rule = rule.Owner;
        }

        if (rule.Comp.GiveBriefing)
            _antag.SendBriefing(spy, Loc.GetString("spy-role-greeting"), null, rule.Comp.GreetSoundNotification);

        return true;
    }

    private string RequestUplink(EntityUid spy, EntityUid mind, string briefing)
    {
        if (_uplink.FindUplinkTarget(spy) is not { } pda)
            return briefing + "\n" + Loc.GetString("spy-role-no-uplink-short");

        EnsureComp<SpyUplinkComponent>(pda).OwnerMind = mind;

        return briefing + "\n" + Loc.GetString("spy-role-uplink-pda-short");
    }

    public bool GetRandomBounty(EntityUid uid, SpyRuleComponent comp)
    {
        if (comp.BountyPool?.Count is null or 0)
            return false;

        var selected = _random.Pick(comp.BountyPool);
        var index = ProtoMan.Index(selected);
        if (!index.Repeatable)
            comp.BountyPool.Remove(selected);
        // TODO no duplicate rewards in 1 bounty list
        var reward = _random.Pick(comp.LootPool[index.Difficulty]);

        var ev = index.Selector.GetEvent();
        RaiseLocalEvent(uid, ev.Initialize(selected, reward));
        return true;
    }

    private void OnPrepend(ref PrependObjectivesSummaryTextEvent args)
    {
        if (_spyUplink.TryGetSpyRoleMind(args.Mind) is not { } role)
            return;

        args.Text += Loc.GetString("spy-role-claimed-bounties", ("name", args.Name), ("amount", role.Comp2.ClaimedBounties));
    }

    [SubscribeLocalEvent]
    private void OnStealTarget(Entity<SpyRuleComponent> ent, ref SpyStealTargetBountySelectorEvent args)
    {
        var target = ProtoMan.Index(args.StealTarget);
        List<NetEntity> validEntities = [];
        var query = EntityQueryEnumerator<StealTargetComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!ent.Comp.StationMaps.Contains(xform.MapID) || comp.StealGroup != args.StealTarget)
                continue;

            validEntities.Add(GetNetEntity(uid));
        }

        if (validEntities.Count == 0)
        {
            Log.Warning($"No valid entities were found for spy bounty {args.Id}");
            ent.Comp.UnavailableBounties.Add(args.Id);
            return;
        }

        ent.Comp.CurrentBounties.Add(new SpyBounty
        {
            ValidEntities = validEntities,
            BountyProto = args.Id,
            Sprite = target.Sprite,
            Name = target.Name,
            Description = "Test Description",
            Reward = args.Reward,
        });
    }

    [SubscribeLocalEvent]
    private void OnPrototype(Entity<SpyRuleComponent> ent, ref SpyPrototypeBountySelectorEvent args)
    {
        // TODO make sure that yaml bounties using this don't need to verify map existance
        var proto = ProtoMan.Index(args.Protos[0]);
        ent.Comp.CurrentBounties.Add(new SpyBounty
        {
            Protos = args.Protos,
            BountyProto = args.Id,
            Name = proto.Name,
            Description = proto.Description,
            Reward = args.Reward,
        });
    }

    [SubscribeLocalEvent]
    private void OnSpecific(Entity<SpyRuleComponent> ent, ref SpySpecificEntityBountySelectorEvent args)
    {
        var proto = ProtoMan.Index(args.Protos[0]);
        var type = Factory.GetComponent(args.QueryComp).GetType();

        Dictionary<string, List<NetEntity>> validEntities = [];

        var depts = args.Areas;

        var query = EntityManager.AllEntityQueryEnumerator(type);
        while (query.MoveNext(out var uid, out _))
        {
            if (ent.Comp.StationMaps.Contains(Transform(uid).MapID) || Prototype(uid) is not { } p || !args.Protos.Contains(p))
                continue;

            if (depts == null)
            {
                validEntities.GetOrNew(string.Empty).Add(GetNetEntity(uid));
                continue;
            }

            if (_area.GetArea(uid) is not { } area || Prototype(area) is not { } areaProto)
                continue;

            if (depts.Count > 0 && !depts.Contains(areaProto.ID))
                continue;

            validEntities.GetOrNew(areaProto.ID).Add(GetNetEntity(uid));
        }

        if (validEntities.Count == 0)
        {
            Log.Warning($"No valid entities were found for spy bounty {args.Id}");
            ent.Comp.UnavailableBounties.Add(args.Id);
            return;
        }

        var list = depts == null ? validEntities[string.Empty] :
            depts.Count == 0 ? _random.Pick(validEntities).Value : validEntities.SelectMany(x => x.Value).ToList();

        ent.Comp.CurrentBounties.Add(new SpyBounty
        {
            ValidEntities = list,
            Protos = args.Protos,
            BountyProto = args.Id,
            Name = proto.Name,
            Description = proto.Description,
            Reward = args.Reward,
        });
    }

    [SubscribeLocalEvent]
    private void OnOrgans(Entity<SpyRuleComponent> ent, ref SpyOrganBountySelectorEvent args)
    {
        var whitelist = args.DepartmentWhitelist;
        var blacklist = args.DepartmentBlacklist;
        var targetOrgan = ProtoMan.Index(_random.Pick(args.ValidOrgans));

        var validPeople = _player.Sessions.Where(IsSessionValid).ToList();
        if (validPeople.Count == 0)
            return;

        var target = _random.Pick(validPeople).AttachedEntity!.Value;

        var organ = _body.GetOrgan(target, targetOrgan);
        if (!IsOrganValid(organ))
            return;

        if (Prototype(organ.Value) is not { } proto)
            return;

        ent.Comp.CurrentBounties.Add(new SpyBounty
        {
            ValidEntities = [GetNetEntity(organ.Value)],
            Protos = [proto.ID],
            BountyProto = args.Id,
            Name = proto.Name,
            Description = proto.Description,
            Reward = args.Reward,
        });

        return;

        bool IsOrganValid([NotNullWhen(true)] EntityUid? organ)
        {
            return organ is not null &&
                !_brainQuery.HasComp(organ) && !_heartQuery.HasComp(organ); // Diona? Slime? Idk
        }

        bool IsSessionValid(ICommonSession session)
        {
            if (session.AttachedEntity is not { } uid)
                return false;

            if (!_humanoidQuery.HasComp(uid))
                return false;

            if (_mobState.IsDead(uid))
                return false;

            if (!IsOrganValid(_body.GetOrgan(uid, targetOrgan)))
                return false;

            if (!_mind.TryGetMind(uid, out var mind, out _) ||
                _role.MindHasRole<SpyRoleComponent>(mind) ||
                !_job.MindTryGetJobId(mind, out var jobId) || jobId is null)
                return false;

            if (blacklist == null && whitelist == null)
                return true;

            if (!_job.TryGetAllDepartments(jobId, out var depts))
                return false;

            var whitelistPass = whitelist == null;
            foreach (var dept in depts)
            {
                if (blacklist?.Contains(dept.ID) is true)
                    return false;

                if (whitelist?.Contains(dept.ID) is not true)
                    continue;

                whitelistPass = true;
                if (blacklist == null)
                    break;
            }

            return whitelistPass;
        }
    }
}

using System.Linq;
using Content.Goobstation.Shared.ManifestListings;
using Content.Server.DoAfter;
using Content.Server.GameTicking;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Roles.Components;
using Content.Shared.Store;
using Content.Shared.Verbs;
using Content.Trauma.Server.GameTicking.Rules.Components;
using Content.Trauma.Shared.Roles;
using Content.Trauma.Shared.Spy;
using Content.Trauma.Shared.Spy.Ui;
using Content.Trauma.Shared.Wizard.FadingTimedDespawn;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Spy;

public sealed partial class SpyUplinkSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private RoleSystem _role = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private ContainerSystem _container = default!;

    [SubscribeLocalEvent]
    private void OnSteal(Entity<SpyUplinkComponent> ent, ref SpyStealDoAfterEvent args)
    {
        RemCompDeferred<ActiveScannerComponent>(ent);

        var protoId = args.Bounty;

        if (args.Cancelled || args.Handled || args.Target is not { } target || !TryGetEntity(args.Rule, out var rule) ||
            !TryComp(rule, out SpyRuleComponent? ruleComp) ||
            ruleComp.CurrentBounties.FirstOrDefault(x => x.BountyProto == protoId) is not { } bounty ||
            !TryGetEntity(args.StealTarget, out var stealTarget) ||
            !IsStealable(target, bounty, out var st) || st != stealTarget.Value ||
            TryGetSpyRole(args.User) is not { } role)
            return;

        // TODO chance to send it to black market when its real
        var despawn = Factory.GetComponent<FadingTimedDespawnComponent>();
        despawn.Lifetime = TimeSpan.Zero;
        despawn.FadeOutTime = TimeSpan.FromSeconds(2);
        AddComp(st, despawn);

        args.Handled = true;

        bounty.Claimed = true;
        role.Comp2.ClaimedBounties++;
        _audio.PlayPvs(ent.Comp.StealEndSound, ent);

        var reward = bounty.Reward;
        var difficulty = ProtoMan.Index(protoId).Difficulty;
        var chanceToRemoveFromPool = ruleComp.ChancesToRemoveRewardFromPool[difficulty];
        if (ProtoMan.HasIndex<SpyRewardPrototype>(reward) &&
            ProtoMan.Index<SpyRewardPrototype>(reward).RemoveFromPoolChanceOverride is { } chance)
            chanceToRemoveFromPool = chance;

        if (_random.Prob(Math.Clamp(chanceToRemoveFromPool, 0f, 1f)))
            ruleComp.LootPool[difficulty].Remove(reward);

        role.Comp2.AvailableRewards.Add(reward);

        RefreshUi(ruleComp.NextRefresh, ruleComp.CurrentBounties);
    }

    [SubscribeLocalEvent]
    private void OnInteract(Entity<SpyUplinkComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanComplexInteract || !args.CanInteract || !args.CanAccess)
            return;

        var target = args.Target;
        var user = args.User;

        if (TryGetSpyRule(user) is not { } rule)
            return;

        args.Verbs.Add(new UtilityVerb
        {
            Priority = 20,
            Act = () => TrySteal(target, ent, user, rule),
            Text = Loc.GetString("spy-uplink-steal-verb"),
        });
    }

    [SubscribeLocalEvent]
    private void OnGetVerb(Entity<SpyUplinkComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanComplexInteract || !args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;

        if (TryGetSpyRule(user) is not { } rule)
            return;

        args.Verbs.Add(new Verb
        {
            Act = () => OpenUi(user, ent, rule),
            Text = Loc.GetString("spy-uplink-open-verb"),
        });
    }

    [SubscribeLocalEvent]
    private void OnCollectReward(Entity<SpyUplinkComponent> ent, ref SpyRewardSelectedMessage args)
    {
        if (!_mind.TryGetMind(args.Actor, out var mind, out _) ||
            TryGetSpyRoleMind(mind) is not { } role ||
            TryGetSpyRule(role.Comp2) is not { } rule ||
            !role.Comp2.AvailableRewards.Contains(args.Id))
            return;

        ListingPrototype? listingProto = null;
        if (ProtoMan.HasIndex<SpyRewardPrototype>(args.Id))
        {
            if (!ProtoMan.Index<SpyRewardPrototype>(args.Id).RewardSelection.Contains(args.Listing))
                return;

            listingProto = ProtoMan.Index(args.Listing);
        }
        else if (args.Id == args.Listing)
            listingProto = ProtoMan.Index(args.Listing);

        role.Comp2.AvailableRewards.Remove(args.Id);

        if (listingProto is not { } proto)
            return;

        // Raise purchase event so that listing appears in roundend screen
        var listing = new ListingDataWithCostModifiers(proto);
        listing.AddCostModifier("spyuplink", listing.Cost.ToDictionary(x => x.Key, x => -x.Value));
        listing.PurchaseAmount =
            CompOrNull<MindListingsComponent>(mind)?.Listings[rule.Id].FirstOrDefault(x => x.ID == listing.ID)?.PurchaseAmount ?? 0;
        listing.PurchaseAmount++;
        var ev = new ListingPurchasedEvent(args.Actor, rule, listing);
        RaiseLocalEvent(mind, ref ev);

        var product = Spawn(proto.ProductEntity, Transform(args.Actor).Coordinates);
        _hands.PickupOrDrop(args.Actor, product);
    }

    [SubscribeLocalEvent]
    private void OnExamine(Entity<SpyUplinkComponent> ent, ref ExaminedEvent args)
    {
        if (!_mind.TryGetMind(args.Examiner, out var mind, out _) || ent.Comp.OwnerMind != mind)
            return;

        args.PushMarkup(Loc.GetString("spy-uplink-examine-message"));
    }

    private void TrySteal(EntityUid uid,
        Entity<SpyUplinkComponent> uplink,
        EntityUid user,
        Entity<SpyRuleComponent?> rule)
    {
        if (!Resolve(rule, ref rule.Comp))
            return;

        if (HasComp<ActiveScannerComponent>(uplink))
            return;

        foreach (var bounty in rule.Comp.CurrentBounties)
        {
            if (!IsStealable(uid, bounty, out var target))
                continue;

            Steal(uid, uplink, user, bounty, rule, target);
            return;
        }

        // TODO steal fail popup
    }

    private void Steal(EntityUid uid,
        Entity<SpyUplinkComponent> uplink,
        EntityUid user,
        SpyBounty bounty,
        EntityUid rule,
        EntityUid stealTarget)
    {
        var proto = ProtoMan.Index(bounty.BountyProto);
        var doArgs = new DoAfterArgs(EntityManager,
            user,
            proto.TheftTime,
            new SpyStealDoAfterEvent(bounty.BountyProto, GetNetEntity(rule), GetNetEntity(stealTarget)),
            uplink,
            uid,
            uplink)
        {
            MultiplyDelay = false,
            BreakOnDropItem = true,
            NeedHand = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
        };

        if (!_doAfter.TryStartDoAfter(doArgs))
            return;

        _audio.PlayPvs(uplink.Comp.StealStartSound, uplink);

        var now = _timing.CurTime;

        var scanner = EnsureComp<ActiveScannerComponent>(uplink);
        scanner.ScannedObject = uid;
        scanner.ScanStartTime = now;
        scanner.ScanEndTime = now + proto.TheftTime;
        Dirty(uplink, scanner);
    }

    private void OpenUi(EntityUid user, EntityUid uplink, Entity<SpyRuleComponent?> rule)
    {
        if (!Resolve(rule, ref rule.Comp))
            return;

        if (!_ui.TryOpenUi(uplink, SpyUplinkUiKey.Key, user))
            return;

        RefreshUplinkUi(uplink, rule.Comp.NextRefresh, rule.Comp.CurrentBounties);
    }

    public void RefreshUi(TimeSpan nextRefresh, HashSet<SpyBounty> currentBounties)
    {
        var query = EntityQueryEnumerator<SpyUplinkComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uplink, out _, out var ui))
        {
            RefreshUplinkUi((uplink, ui), nextRefresh, currentBounties);
        }
    }

    public void RefreshUplinkUi(Entity<UserInterfaceComponent?> ent,
        TimeSpan nextRefresh,
        HashSet<SpyBounty> currentBounties)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var dict = new Dictionary<NetEntity, List<string>>();

        foreach (var actor in _ui.GetActors(ent, SpyUplinkUiKey.Key))
        {
            if (TryGetSpyRole(actor) is not { } role)
                continue;

            dict[GetNetEntity(actor)] = role.Comp2.AvailableRewards;
        }

        var state = new SpyUpdateState(nextRefresh, currentBounties, dict);
        _ui.SetUiState(ent, SpyUplinkUiKey.Key, state);
    }

    public Entity<MindRoleComponent, SpyRoleComponent>? TryGetSpyRoleMind(EntityUid mind)
    {
        if (!_role.MindHasRole<SpyRoleComponent>(mind, out var role))
            return null;

        return role;
    }

    public Entity<MindRoleComponent, SpyRoleComponent>? TryGetSpyRole(EntityUid user)
    {
        if (!_mind.TryGetMind(user, out var mind, out _))
            return null;

        return TryGetSpyRoleMind(mind);
    }

    public EntityUid? TryGetSpyRule(EntityUid user)
    {
        if (TryGetSpyRole(user) is not { } role || role.Comp2.Rule is not { } rule || !_ticker.IsGameRuleActive(rule))
            return null;

        return rule;
    }

    public EntityUid? TryGetSpyRule(SpyRoleComponent role)
    {
        if (role.Rule is not { } rule || !_ticker.IsGameRuleActive(rule))
            return null;

        return rule;
    }

    public bool IsStealable(EntityUid uid, SpyBounty bounty, out EntityUid stealTarget)
    {
        stealTarget = uid;

        if (bounty.Claimed)
            return false;

        if (HasComp<FadingTimedDespawnComponent>(uid))
            return false;

        if (bounty.ValidEntities.Count == 0)
            return bounty.Protos is { } protos && Prototype(uid)?.ID is { } id && protos.Contains(id);

        foreach (var netValid in bounty.ValidEntities)
        {
            var valid = GetEntity(netValid);
            if (valid == uid)
                return true;

            foreach (var container in _container.GetContainingContainers(valid))
            {
                if (container.Owner != uid)
                    continue;

                stealTarget = valid;
                return true;
            }
        }

        return false;
    }
}

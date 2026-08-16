// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Shared.ManifestListings;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Store;
using Content.Shared.Verbs;
using Content.Trauma.Shared.Roles;
using Content.Trauma.Shared.Spy.Ui;
using Content.Trauma.Shared.Wizard.FadingTimedDespawn;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Spy;

public sealed partial class SpyUplinkSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _admin = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedJobSystem _job = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedFadingTimedDespawnSystem _fadeDespawn = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SpyUplinkComponent> ent, ref MapInitEvent args)
    {
        _ui.SetUi(ent.Owner, SpyUplinkUiKey.Key, new InterfaceData("SpyUplinkBoundUserInterface"));
    }

    [SubscribeLocalEvent]
    private void OnAfterAutoHandleState(Entity<SpyRuleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshUi();
    }

    [SubscribeLocalEvent]
    private void OnStateAttempt(Entity<SpyUplinkComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }

    private bool CanGetState(ICommonSession? player)
    {
        if (player is null || !_mind.TryGetMind(player.UserId, out var mind))
            return true;

        return TryGetSpyRoleMind(mind.Value) != null;
    }

    [SubscribeLocalEvent]
    private void OnSteal(Entity<SpyUplinkComponent> ent, ref SpyStealDoAfterEvent args)
    {
        RemCompDeferred<ActiveScannerComponent>(ent);

        var protoId = args.Bounty;

        if (args.Cancelled || args.Handled || args.Target is not { } target || !TryGetEntity(args.Rule, out var rule) ||
            !TryComp(rule, out SpyRuleComponent? ruleComp) ||
            ruleComp.CurrentBounties.FirstOrDefault(x => x.BountyProto == protoId) is not { } bounty ||
            !TryGetEntity(args.StealTarget, out var stealTarget) ||
            !IsStealable(target, bounty, args.User, out var st) || st != stealTarget.Value ||
            TryGetSpyRole(args.User) is not { } role)
            return;

        _admin.Add(LogType.EntityDelete, LogImpact.Low, $"{args.User:user} used spy uplink to steal {st}");

        // TODO chance to send it to black market when its real
        _fadeDespawn.FadeDespawnEntity(st, TimeSpan.Zero, TimeSpan.FromSeconds(2));

        args.Handled = true;

        bounty.Claimed = true;
        role.Comp2.ClaimedBounties++;
        _audio.PlayPredicted(ent.Comp.StealEndSound, ent, args.User);

        var reward = bounty.Reward;
        role.Comp2.AvailableRewards.Add(reward);

        RefreshUi(ent.Owner);

        if (_net.IsClient)
            return;

        var difficulty = ProtoMan.Index(protoId).Difficulty;
        var chanceToRemoveFromPool = ruleComp.ChancesToRemoveRewardFromPool[difficulty];
        if (ProtoMan.TryIndex<SpyRewardPrototype>(reward, out var index) &&
            index.RemoveFromPoolChanceOverride is { } chance)
            chanceToRemoveFromPool = chance;

        if (_random.Prob(Math.Clamp(chanceToRemoveFromPool, 0f, 1f)))
            ruleComp.CachedRewards[difficulty].Remove(reward);
    }

    [SubscribeLocalEvent]
    private void OnInteract(Entity<SpyUplinkComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        var user = args.User;

        if (TryGetSpyRule(user) is not { } rule)
            return;

        args.Handled = true;
        TrySteal(target, ent, user, rule);
    }

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<SpyUplinkComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
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
            Priority = 20,
            Act = () => OpenUi(user, ent, rule),
            Text = Loc.GetString("spy-uplink-open-verb"),
        });
    }

    [SubscribeLocalEvent]
    private void OnGetPdaVerb(Entity<PdaComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanComplexInteract || !args.CanInteract || !args.CanAccess || HasComp<SpyUplinkComponent>(ent))
            return;

        var user = args.User;

        if (TryGetSpyRole(user) is not { } role)
            return;

        args.Verbs.Add(new Verb
        {
            Priority = 20,
            Act = () => MakeUplink(ent, user, role),
            Text = Loc.GetString("spy-uplink-new"),
        });
    }

    private void MakeUplink(EntityUid uplink, EntityUid user, Entity<MindRoleComponent, SpyRoleComponent> role)
    {
        if (role.Comp2.OwnedUplink is { } oldUplink && uplink == oldUplink)
            return;

        var doArgs = new DoAfterArgs(EntityManager,
            user,
            role.Comp2.MakeUplinkTime,
            new SpyMakeUplinkDoAfterEvent(),
            uplink,
            used: uplink)
        {
            MultiplyDelay = false,
            BreakOnDropItem = true,
            NeedHand = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doArgs);
    }

    [SubscribeLocalEvent]
    private void OnNewUplinkDoafter(Entity<PdaComponent> ent, ref SpyMakeUplinkDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        if (HasComp<SpyUplinkComponent>(ent) || !_mind.TryGetMind(args.User, out var mind, out _) ||
            TryGetSpyRoleMind(mind) is not { } role)
            return;

        if (role.Comp2.OwnedUplink is { } oldUplink)
        {
            if (ent.Owner == oldUplink)
                return;

            if (Exists(oldUplink))
                RemCompDeferred<SpyUplinkComponent>(oldUplink);
        }

        var uplink = EnsureComp<SpyUplinkComponent>(ent);
        uplink.OwnerMind = mind;
        Dirty(ent, uplink);

        role.Comp2.OwnedUplink = ent;
        Dirty(role);
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

        var product = PredictedSpawnAtPosition(proto.ProductEntity, Transform(args.Actor).Coordinates);
        _hands.PickupOrDrop(args.Actor, product);

        if (_net.IsClient)
            return;

        // Raise purchase event so that listing appears in roundend screen
        var listing = new ListingDataWithCostModifiers(proto);
        listing.AddCostModifier("spyuplink", listing.Cost.ToDictionary(x => x.Key, x => -x.Value));
        listing.PurchaseAmount =
            CompOrNull<MindListingsComponent>(mind)?.Listings[rule.Id].FirstOrDefault(x => x.ID == listing.ID)?.PurchaseAmount ?? 0;
        listing.PurchaseAmount++;
        var ev = new ListingPurchasedEvent(args.Actor, rule, listing);
        RaiseLocalEvent(mind, ref ev);
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
            if (!IsStealable(uid, bounty, user, out var target))
                continue;

            Steal(uid, uplink, user, bounty, rule, target);
            return;
        }

        var item = Identity.Entity(uid, EntityManager);
        _popup.PopupEntity(Loc.GetString("spy-uplink-steal-fail", ("target", item)), user, user, PopupType.MediumCaution);
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

        _audio.PlayPredicted(uplink.Comp.StealStartSound, uplink, user);

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

        _ui.OpenUi(uplink, SpyUplinkUiKey.Key, user);
        RefreshUi(uplink);
    }

    public void RefreshUi()
    {
        var query = EntityQueryEnumerator<SpyUplinkComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uplink, out _, out var ui))
        {
            RefreshUi((uplink, ui));
        }
    }

    public void RefreshUi(Entity<UserInterfaceComponent?> uplink)
    {
        if (_ui.TryGetOpenUi(uplink, SpyUplinkUiKey.Key, out var bui))
            bui.Update();
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
        if (TryGetSpyRole(user) is not { } role || role.Comp2.Rule is not { } rule)
            return null;

        return rule;
    }

    public EntityUid? TryGetSpyRule(SpyRoleComponent role)
    {
        if (role.Rule is not { } rule)
            return null;

        return rule;
    }

    public bool CanClaim(SpyBounty bounty, EntityUid user)
    {
        if (bounty.Claimed)
            return false;

        if (!ProtoMan.TryIndex(bounty.BountyProto, out var proto))
            return false;

        if (proto.JobBlacklist == null && proto.DepartmentBlacklist == null)
            return true;

        if (!_mind.TryGetMind(user, out var mind, out _))
            return false;

        if (!_job.MindTryGetJobId(mind, out var job) || job == null)
            return true;

        if (proto.JobBlacklist?.Contains(job.Value) is true)
            return false;

        if (proto.DepartmentBlacklist is not { } deptBlacklist)
            return true;

        if (!_job.TryGetAllDepartments(job.Value, out var depts))
            return true;

        return depts.All(x => !deptBlacklist.Contains(x.ID));
    }

    public bool IsStealable(EntityUid uid, SpyBounty bounty, EntityUid user, out EntityUid stealTarget)
    {
        stealTarget = uid;

        if (!CanClaim(bounty, user))
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

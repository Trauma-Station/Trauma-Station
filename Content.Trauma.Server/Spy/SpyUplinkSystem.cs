using Content.Server.DoAfter;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Roles.Components;
using Content.Shared.Verbs;
using Content.Trauma.Server.GameTicking.Rules.Components;
using Content.Trauma.Shared.Roles;
using Content.Trauma.Shared.Spy;
using Content.Trauma.Shared.Spy.Ui;
using Content.Trauma.Shared.Wizard.FadingTimedDespawn;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Spy;

public sealed partial class SpyUplinkSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private RoleSystem _role = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpyUplinkComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SpyUplinkComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<SpyUplinkComponent, BeforeRangedInteractEvent>(OnInteract);
        SubscribeLocalEvent<SpyUplinkComponent, SpyStealDoAfterEvent>(OnSteal);
    }

    private void OnSteal(Entity<SpyUplinkComponent> ent, ref SpyStealDoAfterEvent args)
    {
        RemCompDeferred<ActiveScannerComponent>(ent);

        if (args.Cancelled || args.Handled || args.Target is not { } target || !TryGetEntity(args.Rule, out var rule) ||
            !TryComp(rule, out SpyRuleComponent? ruleComp) || !IsStealable(target, args.Bounty))
            return;

        // TODO chance to send it to black market when its real
        var despawn = EnsureComp<FadingTimedDespawnComponent>(target);
        despawn.Lifetime = 0f;
        despawn.FadeOutTime = 2f;
        Dirty(target, despawn);

        args.Handled = true;

        args.Bounty.Claimed = true;
        _audio.PlayPvs(ent.Comp.StealEndSound, ent);
        RefreshUi(ruleComp.NextRefresh, ruleComp.CurrentBounties);
        // TODO reward
    }

    private void OnInteract(Entity<SpyUplinkComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (!args.CanReach || args.Handled || args.Target is not { } target || HasComp<ActiveScannerComponent>(ent))
            return;

        var user = args.User;

        if (TryGetSpyRule(user) is not { } rule)
            return;

        if (TrySteal(target, ent, user, rule))
            args.Handled = true;
    }

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
            // TODO VERB ICON find a better icon
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
        });
    }

    private void OnExamine(Entity<SpyUplinkComponent> ent, ref ExaminedEvent args)
    {
        if (!_mind.TryGetMind(args.Examiner, out var mind, out _) || ent.Comp.OwnerMind != mind)
            return;

        args.PushMarkup(Loc.GetString("spy-uplink-examine-message"));
    }

    private bool TrySteal(EntityUid uid,
        Entity<SpyUplinkComponent> uplink,
        EntityUid user,
        Entity<SpyRuleComponent?> rule)
    {
        if (!Resolve(rule, ref rule.Comp))
            return false;

        foreach (var bounty in rule.Comp.CurrentBounties)
        {
            if (!IsStealable(uid, bounty))
                continue;

            Steal(uid, uplink, user, bounty, rule);
            return true;
        }

        return false;
    }

    private void Steal(EntityUid uid,
        Entity<SpyUplinkComponent> uplink,
        EntityUid user,
        SpyBounty bounty,
        EntityUid rule)
    {
        var doArgs = new DoAfterArgs(EntityManager,
            user,
            bounty.TheftTime,
            new SpyStealDoAfterEvent(bounty, GetNetEntity(rule)),
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
        scanner.ScanEndTime = now + bounty.TheftTime;
        Dirty(uplink, scanner);
    }

    private void OpenUi(EntityUid user, EntityUid uplink, Entity<SpyRuleComponent?> rule)
    {
        if (!Resolve(rule, ref rule.Comp))
            return;

        if (!_ui.TryOpenUi(uplink, SpyUplinkUiKey.Key, user))
            return;

        var state = new SpyUpdateState(rule.Comp.NextRefresh, rule.Comp.CurrentBounties);
        _ui.SetUiState(uplink, SpyUplinkUiKey.Key, state);
    }

    public void RefreshUi(TimeSpan nextRefresh, HashSet<SpyBounty> currentBounties)
    {
        var state = new SpyUpdateState(nextRefresh, currentBounties);

        var query = EntityQueryEnumerator<SpyUplinkComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uplink, out _, out var ui))
        {
            _ui.SetUiState((uplink, ui), SpyUplinkUiKey.Key, state);
        }
    }

    public Entity<MindRoleComponent, SpyRoleComponent>? TryGetSpyRole(EntityUid user)
    {
        if (!_mind.TryGetMind(user, out var mind, out _) || !_role.MindHasRole<SpyRoleComponent>(mind, out var role))
            return null;

        return role;
    }

    public EntityUid? TryGetSpyRule(EntityUid user)
    {
        if (TryGetSpyRole(user) is not { } role || role.Comp2.Rule is not { } rule || !_ticker.IsGameRuleActive(rule))
            return null;

        return rule;
    }

    public bool IsStealable(EntityUid uid, SpyBounty bounty)
    {
        if (bounty.Claimed)
            return false;

        if (HasComp<FadingTimedDespawnComponent>(uid))
            return false;

        if (bounty.ValidEntities.Count == 0)
            return Prototype(uid)?.ID is { } id && id == bounty.Proto;

        return bounty.ValidEntities.Contains(GetNetEntity(uid));
    }
}

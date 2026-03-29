using Content.Goobstation.Common.Religion;
using Content.Goobstation.Common.Temperature.Components;
using Content.Goobstation.Shared.Bible; // Goobstation - Bible
using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.Popups;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;
using Content.Shared.Verbs;
using Content.Trauma.Common.CosmicCult.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.CosmicCult;

public sealed class CosmicRiftSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedCosmicCultSystem _cult = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CosmicRiftComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BibleComponent, GetVerbsEvent<UtilityVerb>>(AddPurgeVerb);
        SubscribeLocalEvent<CosmicRiftComponent, GetVerbsEvent<AlternativeVerb>>(AddTravelVerb);
        SubscribeLocalEvent<CosmicRiftComponent, EventPurgeRiftDoAfter>(OnPurgeDoAfter);
    }

    #region Base Logic

    private void OnStartup(Entity<CosmicRiftComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.EntropyTimer = _timing.CurTime + ent.Comp.EntropyTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var riftQuery = EntityQueryEnumerator<CosmicRiftComponent>();
        while (riftQuery.MoveNext(out _, out var comp))
        {
            if (_timing.CurTime < comp.EntropyTimer || comp.EntropyStored >= comp.EntropyCap)
                continue;

            comp.EntropyStored++;
            comp.EntropyTimer = _timing.CurTime + comp.EntropyTime;
        }
    }

    #endregion
    #region Verbs

    private void AddPurgeVerb(Entity<BibleComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess
        || !args.CanInteract
        || args.Using == null
        || !TryComp<CosmicRiftComponent>(args.Target, out var riftComp))
            return;

        TimeSpan purgeTime;
        if (HasComp<BibleUserComponent>(args.User) && riftComp.ChaplainTime is { } chaplainTime)
        {
            purgeTime = chaplainTime;
        }
        else if (riftComp.BibleTime is { } bibleTime)
        {
            purgeTime = bibleTime;
        }
        else return;

        var user = args.User;
        var target = args.Target;
        var item = ent;
        var verb = new UtilityVerb()
        {
            Act = () =>
            {
                _popup.PopupClient(Loc.GetString("cosmiccult-rift-beginpurge"), user, user);
                var doargs = new DoAfterArgs(EntityManager,
                    user,
                    purgeTime,
                    new EventPurgeRiftDoAfter(),
                    user,
                    target)
                {
                    DistanceThreshold = 1.5f, BreakOnDamage = true, BreakOnHandChange = false, BreakOnMove = true, MovementThreshold = 0.5f,
                };
                _doAfter.TryStartDoAfter(doargs);
            },
            Text = Loc.GetString("cosmic-cult-verb-rift-purge-name"),
            Message = Loc.GetString("cosmic-cult-verb-rift-purge-desc", ("target", target), ("item", item)),
            IconEntity = GetNetEntity(ent)
        };
        args.Verbs.Add(verb);
    }

    private void AddHarvestVerb(Entity<CosmicRiftComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess
        || !args.CanInteract
        || !TryComp<CosmicCultistComponent>(args.User, out var cultComp))
            return;

        var user = args.User;
        var verb = new ActivationVerb()
        {
            Act = () =>
            {
                var transferred = _cult.AddEntropy((user, cultComp), ent.Comp.EntropyStored);
                ent.Comp.EntropyStored -= transferred;
                _popup.PopupClient(
                    Loc.GetString("cosmic-cult-verb-rift-harvest-popup",
                    ("count", transferred),
                    ("target", ent)),
                    ent, user);
            },
            Text = Loc.GetString("cosmic-cult-verb-rift-harvest-name"),
            Message = Loc.GetString("cosmic-cult-verb-rift-harvest-desc", ("target", ent))
        };
        args.Verbs.Add(verb);
    }

    private void AddTravelVerb(Entity<CosmicRiftComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess
        || !args.CanInteract
        || !HasComp<CosmicCultistComponent>(args.User))
            return;

        var user = args.User;
        var verb = new AlternativeVerb()
        {
            Act = () =>
            {
                var doargs = new DoAfterArgs(EntityManager,
                    user,
                    ent.Comp.TravelTime,
                    new EventTravelRiftDoAfter(),
                    user,
                    user)
                {
                    DistanceThreshold = 1.5f, BreakOnDamage = true, BreakOnHandChange = false, BreakOnMove = true, MovementThreshold = 0.5f,
                };
                _doAfter.TryStartDoAfter(doargs);
            },
            Text = Loc.GetString("cosmic-cult-verb-rift-travel-name"),
            Message = Loc.GetString("cosmic-cult-verb-rift-travel-desc", ("target", ent))
        };
        args.Verbs.Add(verb);
    }

    private void AddUpgradeVerb(Entity<CosmicRiftComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess
        || !args.CanInteract
        || !HasComp<CosmicCultistComponent>(args.User)
        || ent.Comp.UpgradeProto is not { } protoId
        || !_proto.Resolve(protoId, out var proto))
            return;

        var user = args.User;
        var verb = new Verb()
        {
            Act = () =>
            {
                var doargs = new DoAfterArgs(EntityManager,
                    user,
                    ent.Comp.UpgradeTime,
                    new EventUpgradeRiftDoAfter(),
                    user,
                    ent)
                {
                    DistanceThreshold = 1.5f, BreakOnDamage = true, BreakOnHandChange = false, BreakOnMove = true, MovementThreshold = 0.5f,
                };
                _doAfter.TryStartDoAfter(doargs);
            },
            Text = Loc.GetString("cosmic-cult-verb-rift-upgrade-name"),
            Message = Loc.GetString("cosmic-cult-verb-rift-upgrade-desc", ("target", ent))
        };
        args.Verbs.Add(verb);
    }

    private void AddDestroyVerb(Entity<CosmicRiftComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess
        || !args.CanInteract
        || !HasComp<CosmicCultistComponent>(args.User)
        || ent.Comp.CloseTime is not { } closteTime)
            return;

        var user = args.User;
        var verb = new Verb()
        {
            Act = () =>
            {
                var doargs = new DoAfterArgs(EntityManager,
                    user,
                    closteTime,
                    new EventCloseRiftDoAfter(),
                    user,
                    ent)
                {
                    DistanceThreshold = 1.5f, Hidden = true, BreakOnDamage = true, BreakOnHandChange = false, BreakOnMove = true, MovementThreshold = 0.5f,
                };
                _doAfter.TryStartDoAfter(doargs);
            },
            Text = Loc.GetString("cosmic-cult-verb-rift-destroy-name"),
            Message = Loc.GetString("cosmic-cult-verb-rift-destroy-desc", ("target", ent))
        };
        args.Verbs.Add(verb);
    }

    #endregion
    #region DoAfters
/*
    private void OnAbsorbDoAfter(Entity<CosmicCultistComponent> uid, ref EventAbsorbRiftDoAfter args)
    {
        var comp = uid.Comp;
        if (args.Target is not { } target || args.Cancelled || args.Handled || !TryComp<CosmicRiftComponent>(target, out var rift))
            return;

        args.Handled = true;
        var tgtpos = Transform(target).Coordinates;
        Spawn(uid.Comp.AbsorbVFX, tgtpos);
        if (comp.CosmicFragmentationActionEntity == null)
            comp.CosmicFragmentationActionEntity = _actions.AddAction(uid, uid.Comp.CosmicFragmentationAction);
        comp.CosmicEmpowered = true;
        comp.RespecsAvailable++;
        comp.CosmicSiphonQuantity = 2;
        comp.CosmicGlareRange = 8;
        comp.CosmicGlareDuration = TimeSpan.FromSeconds(6);
        comp.CosmicGlareStun = TimeSpan.FromSeconds(0.5);
        comp.CosmicImpositionDuration = TimeSpan.FromSeconds(7.2);
        comp.CosmicStrideDuration = TimeSpan.FromSeconds(7);
        Dirty(uid, comp);
        EnsureComp<PressureImmunityComponent>(args.User);
        EnsureComp<SpecialLowTempImmunityComponent>(args.User);
        EnsureComp<CosmicNonRespiratingComponent>(args.User);
        RemComp<HungerComponent>(args.User); // Eschew Metabolism is kill, rifts give the effect instead
        RemComp<ThirstComponent>(args.User);
        _popup.PopupCoordinates(
            Loc.GetString("cosmiccult-rift-absorb", ("NAME", Identity.Entity(args.Args.User, EntityManager))),
            Transform(args.Args.User).Coordinates,
            PopupType.MediumCaution);
        QueueDel(target);

        if (comp.CosmicShopActionEntity is { } shop)
            _ui.SetUiState(shop, CosmicShopKey.Key, new CosmicShopBuiState());
    }
*/
    private void OnPurgeDoAfter(Entity<CosmicRiftComponent> uid, ref EventPurgeRiftDoAfter args)
    {
        if (args.Args.Target == null || args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        var tgtpos = Transform(uid).Coordinates;
        Spawn(uid.Comp.PurgeVFX, tgtpos);
        _audio.PlayPvs(uid.Comp.PurgeSound, args.User);
        _popup.PopupCoordinates(
            Loc.GetString("cosmiccult-rift-purge", ("NAME", Identity.Entity(args.Args.User, EntityManager))),
            Transform(args.Args.User).Coordinates,
            PopupType.Medium);
        QueueDel(uid);
    }
    #endregion
}

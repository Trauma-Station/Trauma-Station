using Content.Goobstation.Common.Religion;
using Content.Goobstation.Common.Temperature.Components;
using Content.Server._DV.CosmicCult.Abilities;
using Content.Server._DV.CosmicCult.Components;
using Content.Server.Actions;
using Content.Server.Atmos.Components;
using Content.Goobstation.Shared.Bible; // Goobstation - Bible
using Content.Server.Popups;
using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Actions.Components;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._DV.CosmicCult.EntitySystems;

public sealed class CosmicRiftSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedCosmicCultSystem _cult = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly CosmicBlankSystem _blank = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private readonly HashSet<Entity<HumanoidProfileComponent>> _targets = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CosmicMalignRiftComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CosmicMalignRiftComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<CosmicMalignRiftComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CosmicCultComponent, EventAbsorbRiftDoAfter>(OnAbsorbDoAfter);
        SubscribeLocalEvent<CosmicMalignRiftComponent, EventPurgeRiftDoAfter>(OnPurgeDoAfter);
    }

    private void OnStartup(Entity<CosmicMalignRiftComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.DangerWait is not { } dangerWait) return;
        ent.Comp.DangerTimer = _timing.CurTime + dangerWait;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var riftQuery = EntityQueryEnumerator<CosmicMalignRiftComponent>();
        while (riftQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.DangerWait is not { } dangerWait || _timing.CurTime < comp.DangerTimer)
                continue;

            comp.DangerTimer = _timing.CurTime + dangerWait;

            _targets.Clear();
            _lookup.GetEntitiesInRange(Transform(uid).Coordinates, comp.DangerRange, _targets);
            _targets.RemoveWhere(target =>
            {
                if (_cult.EntityIsCultist(target)) return true;

                var evt = new CosmicAbilityAttemptEvent(target, true);
                RaiseLocalEvent(ref evt);
                return evt.Cancelled;
            });

            foreach (var target in _targets)
            {
                if (_random.Prob(0.5f))
                {
                    EnsureComp<CosmicSubtleMarkComponent>(target, out var markComp); // Give them a subtle mark for a few minutes to make everyone think they're a cultist
                    markComp.ExpireTimer = _timing.CurTime + comp.DangerTimer;
                }
                if (_random.Prob(0.2f))
                    _blank.ShuntTarget(target, TimeSpan.FromSeconds(25));
            }
        }
    }

    private void OnInteract(Entity<CosmicMalignRiftComponent> uid, ref InteractHandEvent args)
    {
        if (args.Handled
            || uid.Comp.Occupied)
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-inuse"), args.User, args.User);
            return;
        }

        if (HasComp<BibleUserComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-chaplainoops"), args.User, args.User);
            return;
        }

        if (!TryComp<CosmicCultComponent>(args.User, out var cultist))
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-invaliduser"), args.User, args.User);
            return;
        }

        args.Handled = true;
        uid.Comp.Occupied = true;
        _popup.PopupEntity(Loc.GetString("cosmiccult-rift-beginabsorb"), args.User, args.User);
        var doargs = new DoAfterArgs(EntityManager,
            args.User,
            uid.Comp.AbsorbTime,
            new EventAbsorbRiftDoAfter(),
            args.User,
            uid)
        {
            DistanceThreshold = 1.5f, Hidden = true, BreakOnDamage = true, BreakOnHandChange = false, BreakOnMove = true, MovementThreshold = 0.5f,
        };
        _doAfter.TryStartDoAfter(doargs);
    }

    private void OnInteractUsing(Entity<CosmicMalignRiftComponent> uid, ref InteractUsingEvent args)
    {
        if (args.Handled
            || uid.Comp.Occupied)
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-inuse"), args.User, args.User);
            return;
        }

        if (HasComp<BibleComponent>(args.Used))
        {
            uid.Comp.Occupied = true;
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-beginpurge"), args.User, args.User);
            var doargs = new DoAfterArgs(EntityManager,
                args.User,
                HasComp<BibleUserComponent>(args.User) ? uid.Comp.ChaplainTime : uid.Comp.BibleTime, // Chap gets a speed boost for purging rifts
                new EventPurgeRiftDoAfter(),
                uid,
                uid)
            {
                DistanceThreshold = 1.5f, Hidden = false, BreakOnDamage = true, BreakOnDropItem = true,
                BreakOnMove = true, MovementThreshold = 2f,
            };
            _doAfter.TryStartDoAfter(doargs);
        }
    }

    private void OnAbsorbDoAfter(Entity<CosmicCultComponent> uid, ref EventAbsorbRiftDoAfter args)
    {
        var comp = uid.Comp;
        if (args.Target is not { } target || args.Cancelled || args.Handled || !TryComp<CosmicMalignRiftComponent>(target, out var rift))
            return;

        args.Handled = true;
        rift.Occupied = false;
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
        _cult.AddEntropy(uid, rift.EntropyGranted);
        _popup.PopupCoordinates(
            Loc.GetString("cosmiccult-rift-absorb", ("NAME", Identity.Entity(args.Args.User, EntityManager))),
            Transform(args.Args.User).Coordinates,
            PopupType.MediumCaution);
        QueueDel(target);

        if (comp.CosmicShopActionEntity is { } shop)
            _ui.SetUiState(shop, CosmicShopKey.Key, new CosmicShopBuiState());
    }

    private void OnPurgeDoAfter(Entity<CosmicMalignRiftComponent> uid, ref EventPurgeRiftDoAfter args)
    {
        if (args.Args.Target == null || args.Cancelled || args.Handled)
        {
            uid.Comp.Occupied = false;
            return;
        }

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
}

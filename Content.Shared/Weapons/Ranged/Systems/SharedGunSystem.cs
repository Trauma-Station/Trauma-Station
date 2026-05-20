// <Trauma>
using Content.Goobstation.Common.Weapons.Multishot;
using Content.Goobstation.Common.Weapons.Ranged;
using Content.Lavaland.Common.Weapons.Ranged;
using Content.Shared.Mech.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Trauma.Common.Projectiles;
// </Trauma>
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.CombatMode;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.Item;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private RechargeBasicEntityAmmoSystem _recharge = default!;
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] protected DamageableSystem Damageable = default!;
    [Dependency] protected ExamineSystemShared Examine = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected IMapManager MapManager = default!;
    [Dependency] protected IPrototypeManager ProtoManager = default!;
    //[Dependency] protected IRobustRandom Random = default!; // Trauma - predicted Random(uid) used instead
    [Dependency] protected ISharedAdminLogManager Logs = default!;
    [Dependency] protected SharedActionsSystem Actions = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] protected SharedContainerSystem Containers = default!;
    [Dependency] protected SharedPhysicsSystem Physics = default!;
    [Dependency] protected SharedPointLightSystem Lights = default!;
    [Dependency] protected SharedPopupSystem PopupSystem = default!;
    [Dependency] protected SharedProjectileSystem Projectiles = default!;
    [Dependency] protected SharedTransformSystem TransformSystem = default!;
    [Dependency] protected TagSystem TagSystem = default!;
    [Dependency] protected ThrowingSystem ThrowingSystem = default!;

    /// <summary>
    /// Default projectile speed
    /// </summary>
    public const float ProjectileSpeed = 40f;

    /// <summary>
    ///     Name of the container slot used as the gun's chamber
    /// </summary>
    public const string ChamberSlot = "gun_chamber";

    /// <summary>
    ///     Name of the container slot used as the gun's magazine
    /// </summary>
    public const string MagazineSlot = "gun_magazine";

    private static readonly ProtoId<TagPrototype> TrashTag = "Trash";

    private const float InteractNextFire = 0.3f;
    private const double SafetyNextFire = 0.5;
    private const float EjectOffset = 0.4f;
    public const string AmmoExamineColor = "yellow"; // Trauma - made public
    protected const string FireRateExamineColor = "yellow";
    public const string ModeExamineColor = "cyan";

    public override void Initialize()
    {
        SubscribeAllEvent<RequestShootEvent>(OnShootRequest);
        SubscribeAllEvent<RequestStopShootEvent>(OnStopShootRequest);
        SubscribeLocalEvent<GunComponent, MeleeHitEvent>(OnGunMelee);

        // Ammo providers
        InitializeBallistic();
        InitializeBattery();
        InitializeCartridge();
        InitializeChamberMagazine();
        InitializeMagazine();
        InitializeRevolver();
        InitializeBasicEntity();
        InitializeClothing();
        InitializeContainer();
        InitializeSolution();
        InitializeGoob(); // Goob

        // Interactions
        SubscribeLocalEvent<GunComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
        SubscribeLocalEvent<GunComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<GunComponent, CycleModeEvent>(OnCycleMode);
        SubscribeLocalEvent<GunComponent, HandSelectedEvent>(OnGunSelected);
        SubscribeLocalEvent<GunComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<GunComponent> gun, ref MapInitEvent args)
    {
#if DEBUG
        if (gun.Comp.NextFire > Timing.CurTime)
            Log.Warning($"Initializing a map that contains an entity that is on cooldown. Entity: {ToPrettyString(gun)}");

        DebugTools.Assert((gun.Comp.AvailableModes & gun.Comp.SelectedMode) != 0x0);
#endif

        RefreshModifiers((gun, gun));
    }

    private void OnGunMelee(Entity<GunComponent> ent, ref MeleeHitEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(ent, out var melee))
            return;

        if (melee.NextAttack > ent.Comp.NextFire)
        {
            ent.Comp.NextFire = melee.NextAttack;
            DirtyField(ent.AsNullable(), nameof(GunComponent.NextFire));
        }
    }

    private void OnShootRequest(RequestShootEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        // <Trauma> - mech relaying, check gun below, multishot, lock-on targeting
        if (user == null ||
            !_combatMode.IsInCombatMode(user))
            return;

        if (TryComp<MechPilotComponent>(user.Value, out var mechPilot))
            user = mechPilot.Mech;

        if (!TryGetGun(user.Value, out var gun))
            return;

        if (HasComp<MultishotComponent>(gun)) // let multishot system handle it
            return;

        gun.Comp.ShootCoordinates = GetCoordinates(msg.Coordinates);
        var potentialTarget = GetEntity(msg.Target);
        if (gun.Comp.Target == null || !gun.Comp.BurstActivated || !gun.Comp.LockOnTargetBurst)
            gun.Comp.Target = potentialTarget;
        AttemptShoot(user.Value, gun);
        if (msg.Continuous)
            gun.Comp.ShotCounter = 0;
        // </Trauma>
    }

    private void OnStopShootRequest(RequestStopShootEvent ev, EntitySessionEventArgs args)
    {
        var gunUid = GetEntity(ev.Gun);

        // <Trauma> - evil shitcode for mechs :(
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        if (TryComp<MechPilotComponent>(user, out var mechPilot))
            user = mechPilot.Mech;

        if (!TryGetGun(user, out var userGun))
            return;

        if (gunUid != userGun.Owner)
        // </Trauma>
            return;

        StopShooting(userGun);
    }

    public bool CanShoot(GunComponent component)
    {
        if (component.NextFire > Timing.CurTime)
            return false;

        return true;
    }

    /// <summary>
    ///     Tries to get an entity with <see cref="GunComponent"/> from the specified entity's hands, or from the entity itself.
    /// </summary>
    /// <param name="entity">Entity that is holding the gun, or is the gun</param>
    /// <param name="gun">Gun entity to return</param>
    /// <returns>True if gun was found</returns>
    public bool TryGetGun(EntityUid entity, out Entity<GunComponent> gun)
    {
        gun = default;

        // <Trauma> - mech shitcode
        if (TryComp<MechComponent>(entity, out var mech) &&
            mech.CurrentSelectedEquipment is {} mechGun &&
            TryComp<GunComponent>(mechGun, out var mechGunComp))
        {
            gun = (mechGun, mechGunComp);
            return true;
        }
        // </Trauma>

        if (_hands.GetActiveItem(entity) is { } held &&
            TryComp(held, out GunComponent? gunComp))
        {
            gun = (held, gunComp);
            return true;
        }

        // <Trauma> - support glove guns
        if (_inventory.TryGetSlotEntity(entity, "gloves", out var gloves) &&
            TryComp<GunComponent>(gloves.Value, out var glovesGun))
        {
            gun = (gloves.Value, glovesGun);
            return true;
        }
        // </Trauma>

        // Last resort is check if the entity itself is a gun.
        if (TryComp(entity, out gunComp))
        {
            gun = (entity, gunComp);
            return true;
        }

        return false;
    }

    private void StopShooting(Entity<GunComponent> ent)
    {
        if (ent.Comp.ShotCounter == 0)
            return;

        ent.Comp.ShotCounter = 0;
        ent.Comp.ShootCoordinates = null;
        if (!ent.Comp.LockOnTargetBurst || !ent.Comp.BurstActivated) // Trauma - check these before resetting target
            ent.Comp.Target = null;
        DirtyField(ent.AsNullable(), nameof(GunComponent.ShotCounter));
    }

    /// <summary>
    /// Attempts to shoot at the target coordinates. Resets the shot counter after every shot.
    /// </summary>
    public bool AttemptShoot(EntityUid user, Entity<GunComponent> gun, EntityCoordinates toCoordinates, EntityUid? target = null)
    {
        gun.Comp.ShootCoordinates = toCoordinates;
        gun.Comp.Target = target;
        var result = AttemptShoot(user, gun);
        gun.Comp.ShotCounter = 0;
        DirtyField(gun.AsNullable(), nameof(GunComponent.ShotCounter));
        return result;
    }

    // Goobstation - Crawling turret fix
    public void AttemptShoot(EntityUid user, Entity<GunComponent> gun, EntityCoordinates toCoordinates, EntityUid target)
    {
        gun.Comp.Target = target;
        gun.Comp.ShootCoordinates = toCoordinates;
        AttemptShoot(user, gun);
        gun.Comp.ShotCounter = 0;
    }

    /// <summary>
    /// Shoots by assuming the gun is the user at default coordinates.
    /// </summary>
    public bool AttemptShoot(Entity<GunComponent> gun)
    {
        var coordinates = new EntityCoordinates(gun, gun.Comp.DefaultDirection);
        gun.Comp.ShootCoordinates = coordinates;
        var result = AttemptShoot(gun, gun);
        gun.Comp.ShotCounter = 0;
        return result;
    }

    private bool AttemptShoot(EntityUid user, Entity<GunComponent> gun)
    {
        if (gun.Comp.FireRateModified <= 0f ||
            !_actionBlockerSystem.CanAttack(user))
        {
            return false;
        }

        var toCoordinates = gun.Comp.ShootCoordinates;

        if (toCoordinates == null)
            return false;

        var curTime = Timing.CurTime;

        // check if anything wants to prevent shooting
        var prevention = new ShotAttemptedEvent
        {
            User = user,
            Used = gun
        };
        RaiseLocalEvent(gun, ref prevention);
        if (prevention.Cancelled)
            return false;

        RaiseLocalEvent(user, ref prevention);
        if (prevention.Cancelled)
            return false;

        // Need to do this to play the clicking sound for empty automatic weapons
        // but not play anything for burst fire.
        if (gun.Comp.NextFire > curTime)
            return false;

        var fireRate = TimeSpan.FromSeconds(1f / gun.Comp.FireRateModified);

        if (gun.Comp.SelectedMode == SelectiveFire.Burst || gun.Comp.BurstActivated)
            fireRate = TimeSpan.FromSeconds(1f / gun.Comp.BurstFireRateModified); // Trauma - use Modified field

        // First shot
        // Previously we checked shotcounter but in some cases all the bullets got dumped at once
        // curTime - fireRate is insufficient because if you time it just right you can get a 3rd shot out slightly quicker.
        if (gun.Comp.NextFire < curTime - fireRate || gun.Comp.ShotCounter == 0 && gun.Comp.NextFire < curTime)
            gun.Comp.NextFire = curTime;

        bool isRechargingGun = HasComp<RechargeBasicEntityAmmoComponent>(gun); // Goobstation

        var shots = 0;
        var lastFire = gun.Comp.NextFire;

        while (gun.Comp.NextFire <= curTime)
        {
            gun.Comp.NextFire += fireRate;
            shots++;
        }

        // NextFire has been touched regardless so need to dirty the gun.
        DirtyField(gun.AsNullable(), nameof(GunComponent.NextFire));

        // Get how many shots we're actually allowed to make, due to clip size or otherwise.
        // Don't do this in the loop so we still reset NextFire.
        if (!gun.Comp.BurstActivated)
        {
            switch (gun.Comp.SelectedMode)
            {
                case SelectiveFire.SemiAuto:
                    shots = Math.Min(shots, 1 - gun.Comp.ShotCounter);
                    break;
                case SelectiveFire.Burst:
                    shots = Math.Min(shots, gun.Comp.ShotsPerBurstModified - gun.Comp.ShotCounter);
                    break;
                case SelectiveFire.FullAuto:
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"No implemented shooting behavior for {gun.Comp.SelectedMode}!");
            }
        }
        else
        {
            shots = Math.Min(shots, gun.Comp.ShotsPerBurstModified - gun.Comp.ShotCounter);
        }

        var attemptEv = new AttemptShootEvent(user, null);
        RaiseLocalEvent(gun, ref attemptEv);

        if (attemptEv.Cancelled)
        {
            if (attemptEv.Message != null)
            {
                PopupSystem.PopupClient(attemptEv.Message, gun, user);
            }
            // <Trauma>
            if (!gun.Comp.LockOnTargetBurst || gun.Comp.ShootCoordinates == null)
                gun.Comp.Target = null;
            // </Trauma>
            gun.Comp.BurstActivated = false;
            gun.Comp.BurstShotsCount = 0;
            gun.Comp.NextFire = TimeSpan.FromSeconds(Math.Max(lastFire.TotalSeconds + SafetyNextFire, gun.Comp.NextFire.TotalSeconds));
            return false;
        }

        var fromCoordinates = Transform(user).Coordinates;
        // Remove ammo
        var ev = new TakeAmmoEvent(shots, [], fromCoordinates, user);

        // Listen it just makes the other code around it easier if shots == 0 to do this.
        if (shots > 0)
            RaiseLocalEvent(gun, ev);

        DebugTools.Assert(ev.Ammo.Count <= shots);
        DebugTools.Assert(shots >= 0);
        UpdateAmmoCount(gun);

        // Even if we don't actually shoot update the ShotCounter. This is to avoid spamming empty sounds
        // where the gun may be SemiAuto or Burst.
        gun.Comp.ShotCounter += shots;
        DirtyField(gun.AsNullable(), nameof(GunComponent.ShotCounter));

        if (ev.Ammo.Count <= 0)
        {
            // triggers effects on the gun if it's empty
            var emptyGunShotEvent = new OnEmptyGunShotEvent(user);
            RaiseLocalEvent(gun, ref emptyGunShotEvent);

            // <Trauma>
            if (isRechargingGun)
            {
                gun.Comp.NextFire = lastFire; // for empty PKAs, don't play no-ammo sound and don't trigger the reload
                return false;
            }

            if (!gun.Comp.LockOnTargetBurst || gun.Comp.ShootCoordinates == null)
                gun.Comp.Target = null;
            // </Trauma>
            gun.Comp.BurstActivated = false;
            gun.Comp.BurstShotsCount = 0;
            gun.Comp.NextFire += TimeSpan.FromSeconds(gun.Comp.BurstCooldownModified); // Trauma - use Modified field

            // Play empty gun sounds if relevant
            // If they're firing an existing clip then don't play anything.
            if (shots > 0)
            {
                PopupSystem.PopupCursor(ev.Reason ?? Loc.GetString("gun-magazine-fired-empty"));

                // Don't spam safety sounds at gun fire rate, play it at a reduced rate.
                // May cause prediction issues? Needs more tweaking
                gun.Comp.NextFire = TimeSpan.FromSeconds(Math.Max(lastFire.TotalSeconds + SafetyNextFire, gun.Comp.NextFire.TotalSeconds));
                Audio.PlayPredicted(gun.Comp.SoundEmpty, gun, user);
                return false;
            }

            return false;
        }

        // Handle burstfire
        if (gun.Comp.SelectedMode == SelectiveFire.Burst)
        {
            gun.Comp.BurstActivated = true;
        }
        if (gun.Comp.BurstActivated)
        {
            gun.Comp.BurstShotsCount += shots;
            if (gun.Comp.BurstShotsCount >= gun.Comp.ShotsPerBurstModified)
            {
                gun.Comp.NextFire += TimeSpan.FromSeconds(gun.Comp.BurstCooldownModified); // Trauma - use Modified field
                // <Trauma>
                if (!gun.Comp.LockOnTargetBurst || gun.Comp.ShootCoordinates == null)
                    gun.Comp.Target = null;
                // </Trauma>
                gun.Comp.BurstActivated = false;
                gun.Comp.BurstShotsCount = 0;
            }
        }

        // Shoot confirmed - sounds also played here in case it's invalid (e.g. cartridge already spent).
        Shoot(gun, ev.Ammo, fromCoordinates, toCoordinates.Value, out var userImpulse, user, throwItems: attemptEv.ThrowItems);
        var shotEv = new GunShotEvent(user, ev.Ammo);
        RaiseLocalEvent(gun, ref shotEv);

        UpdateAmmoCount(gun); //GoobStation - Multishot
        if (!userImpulse || !TryComp<PhysicsComponent>(user, out var userPhysics))
            return true;

        var shooterEv = new ShooterImpulseEvent();
        RaiseLocalEvent(user, ref shooterEv);

        if (shooterEv.Push)
            CauseImpulse(fromCoordinates, toCoordinates.Value, (user, userPhysics));
        return true;
    }

    public void Shoot(
        Entity<GunComponent> gun,
        EntityUid ammo,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        out bool userImpulse,
        EntityUid? user = null,
        bool throwItems = false)
    {
        var shootable = EnsureShootable(ammo);
        Shoot(gun, new List<(EntityUid? Entity, IShootable Shootable)>(1) { (ammo, shootable) }, fromCoordinates, toCoordinates, out userImpulse, user, throwItems);
    }

    /// <summary>
    /// Trauma - moved server version here and predicted it
    /// </summary>
    public void Shoot(
        Entity<GunComponent> gun,
        List<(EntityUid? Entity, IShootable Shootable)> ammo,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        out bool userImpulse,
        EntityUid? user = null,
        bool throwItems = false)
    {
        userImpulse = false;

        if (user != null)
        {
            var selfEvent = new SelfBeforeGunShotEvent(user.Value, gun, ammo);
            RaiseLocalEvent(user.Value, selfEvent);
            if (selfEvent.Cancelled)
                return;
        }

        var fromMap = TransformSystem.ToMapCoordinates(fromCoordinates);
        var toMap = TransformSystem.ToMapCoordinates(toCoordinates).Position;
        var mapDirection = toMap - fromMap.Position;
        // <Trauma> - prevent shooting with 0,0 direction
        if (mapDirection == Vector2.Zero)
            return;
        var recoilScale = GetRecoilScale(user, gun);
        var mapAngle = mapDirection.ToAngle();
        var angle = GetRecoilAngle(Timing.CurTime, gun, mapDirection.ToAngle(), user, recoilScale); // Trauma - pass user
        // </Trauma>

        userImpulse = true;

        // If applicable, this ensures the projectile is parented to grid on spawn, instead of the map.
        var fromEnt = MapManager.TryFindGridAt(fromMap, out var gridUid, out _)
            ? TransformSystem.WithEntityId(fromCoordinates, gridUid)
            : new EntityCoordinates(_map.GetMapOrInvalid(fromMap.MapId), fromMap.Position);

        var toMapBeforeRecoil = toMap; // Goobstation

        // Update shot based on the recoil
        toMap = fromMap.Position + angle.ToVec() * mapDirection.Length();
        mapDirection = toMap - fromMap.Position;
        var gunVelocity = Physics.GetMapLinearVelocity(fromEnt);

        // I must be high because this was getting tripped even when true.
        // DebugTools.Assert(direction != Vector2.Zero);
        var shotProjectiles = new List<EntityUid>(ammo.Count);

        foreach (var (ent, shootable) in ammo)
        {
            // pneumatic cannon doesn't shoot bullets it just throws them, ignore ammo handling
            if (throwItems && ent != null)
            {
                ShootOrThrow(ent.Value, mapDirection, gunVelocity, gun, user, targetCoordinates: toMapBeforeRecoil);
                shotProjectiles.Add(ent.Value); // Goobstation
                continue;
            }

            // TODO: Clean this up in a gun refactor at some point - too much copy pasting
            switch (shootable)
            {
                // Cartridge shoots something else
                case CartridgeAmmoComponent cartridge:
                    if (!cartridge.Spent)
                    {
                        var uid = PredictedSpawnAtPosition(cartridge.Prototype, fromEnt);
                        // <Trauma>
                        var cartEv = new CartridgeFiredEvent(uid);
                        RaiseLocalEvent(ent!.Value, ref cartEv);
                        // </Trauma>
                        CreateAndFireProjectiles(uid, cartridge);

                        RaiseLocalEvent(ent!.Value, new AmmoShotEvent()
                        {
                            FiredProjectiles = shotProjectiles,
                        });

                        SetCartridgeSpent(ent.Value, cartridge, true);

                        if (cartridge.DeleteOnSpawn)
                            PredictedDel(ent.Value);
                    }
                    else
                    {
                        userImpulse = false;
                        Audio.PlayPredicted(gun.Comp.SoundEmpty, gun, user);
                    }

                    // Something like ballistic might want to leave it in the container still
                    if (!cartridge.DeleteOnSpawn && !Containers.IsEntityInContainer(ent!.Value))
                        EjectCartridge(Random(gun), user, ent.Value, angle); // Trauma - predicted this

                    Dirty(ent!.Value, cartridge);
                    break;
                // Ammo shoots itself
                case AmmoComponent newAmmo:
                    if (ent == null)
                        break;
                    CreateAndFireProjectiles(ent.Value, newAmmo);

                    break;
                case HitscanAmmoComponent:
                    if (ent == null)
                        break;

                    var hitscanEv = new HitscanTraceEvent
                    {
                        FromCoordinates = fromCoordinates,
                        TargetCoordinates = toMapBeforeRecoil, // Goob
                        ShotDirection = mapDirection.Normalized(),
                        Gun = gun,
                        Shooter = user,
                        Target = gun.Comp.Target,
                    };
                    RaiseLocalEvent(ent.Value, ref hitscanEv);
                    PredictedDel(ent);

                    Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, user);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // <Trauma>
            if (userImpulse)
                Recoil(user, mapDirection, gun.Comp.CameraRecoilScalarModified * recoilScale);
            // </Trauma>
        }

        RaiseLocalEvent(gun, new AmmoShotEvent()
        {
            FiredProjectiles = shotProjectiles,
        });

        // <Goob>
        if (user.HasValue)
        {
            var userEv = new AmmoShotUserEvent(gun, shotProjectiles);
            RaiseLocalEvent(user.Value, ref userEv);
        }
        // </Goob>

        void CreateAndFireProjectiles(EntityUid ammoEnt, AmmoComponent ammoComp)
        {
            if (TryComp<ProjectileSpreadComponent>(ammoEnt, out var ammoSpreadComp))
            {
                var spreadEvent = new GunGetAmmoSpreadEvent(ammoSpreadComp.Spread);
                RaiseLocalEvent(gun, ref spreadEvent);

                var angles = LinearSpread(mapAngle - spreadEvent.Spread / 2,
                    mapAngle + spreadEvent.Spread / 2, ammoSpreadComp.Count);

                ShootOrThrow(ammoEnt, angles[0].ToVec(), gunVelocity, gun, user, targetCoordinates: toMapBeforeRecoil); // Trauma - add target coords
                shotProjectiles.Add(ammoEnt);

                for (var i = 1; i < ammoSpreadComp.Count; i++)
                {
                    var newuid = PredictedSpawnAtPosition(ammoSpreadComp.Proto, fromEnt);
                    // <Trauma>
                    var pelletEv = new SpreadPelletFiredEvent(newuid);
                    RaiseLocalEvent(ammoEnt, ref pelletEv);
                    SetProjectilePerfectHitEntities(newuid, user, new MapCoordinates(toMap, fromMap.MapId));
                    // </Trauma>
                    ShootOrThrow(newuid, angles[i].ToVec(), gunVelocity, gun, user, targetCoordinates: toMapBeforeRecoil); // Trauma - add target coords
                    shotProjectiles.Add(newuid);
                }
            }
            else
            {
                ShootOrThrow(ammoEnt, mapDirection, gunVelocity, gun, user, targetCoordinates: toMapBeforeRecoil); // Trauma - add target coords
                shotProjectiles.Add(ammoEnt);
            }

            MuzzleFlash(gun, ammoComp, mapDirection.ToAngle(), user);
            Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, user);
        }
    }

    public void ShootProjectile(EntityUid uid, Vector2 direction, Vector2 gunVelocity, EntityUid? gunUid, EntityUid? user = null, float speed = ProjectileSpeed,
        Vector2? targetCoordinates = null) // Trauma
    {
        var physics = EnsureComp<PhysicsComponent>(uid);
        Physics.SetBodyStatus(uid, physics, BodyStatus.InAir);

        var targetMapVelocity = gunVelocity + direction.Normalized() * speed;
        var currentMapVelocity = Physics.GetMapLinearVelocity(uid, physics);
        var finalLinear = physics.LinearVelocity + targetMapVelocity - currentMapVelocity;
        Physics.SetLinearVelocity(uid, finalLinear, body: physics);

        var projectile = EnsureComp<ProjectileComponent>(uid);
        projectile.Weapon = gunUid;
        var shooter = user ?? gunUid;
        if (shooter != null)
            Projectiles.SetShooter(uid, projectile, shooter.Value);

        Physics.UpdateIsPredicted(uid, physics); // Trauma - predict this shit

        TransformSystem.SetWorldRotation(uid, direction.ToWorldAngle() + projectile.Angle);
        // <Trauma>
        if (targetCoordinates is {} target)
            projectile.TargetCoordinates = target;

        if (user is {} userUid)
        {
            var ev = new PlayerShotProjectileEvent(uid, userUid);
            RaiseLocalEvent(ref ev);
        }
        if (gunUid is {} gun)
        {
            var shotEv = new ProjectileShotEvent(uid);
            RaiseLocalEvent(gun, ref shotEv);
        }
        // </Trauma>
    }

    protected abstract void Popup(string message, EntityUid? uid, EntityUid? user);

    /// <summary>
    /// Call this whenever the ammo count for a gun changes.
    /// </summary>
    public virtual void UpdateAmmoCount(EntityUid uid, bool prediction = true) {} // Trauma - made public

    protected void SetCartridgeSpent(EntityUid uid, CartridgeAmmoComponent cartridge, bool spent)
    {
        if (cartridge.Spent != spent)
            DirtyField(uid, cartridge, nameof(CartridgeAmmoComponent.Spent));

        cartridge.Spent = spent;
        Appearance.SetData(uid, AmmoVisuals.Spent, spent);

        if (!cartridge.MarkSpentAsTrash)
            return;

        if (spent)
            TagSystem.AddTag(uid, TrashTag);
        else
            TagSystem.RemoveTag(uid, TrashTag);
    }

    /// <summary>
    /// Drops a single cartridge / shell
    /// </summary>
    public void EjectCartridge( // Trauma - made public
        // <Trauma>
        System.Random rand, // predicted random instance for the gun
        EntityUid? user,
        // </Trauma>
        EntityUid entity,
        Angle? angle = null,
        bool playSound = true)
    {
        // TODO: Sound limit version.
        // <Trauma> - use random from params
        var offsetPos = rand.NextAngle().RotateVec(new Vector2(rand.NextFloat(0, EjectOffset), 0));
        var xform = Transform(entity);

        var coordinates = xform.Coordinates;
        coordinates = coordinates.Offset(offsetPos);

        TransformSystem.SetLocalRotation(entity, rand.NextAngle(), xform);
        // </Trauma>
        TransformSystem.SetCoordinates(entity, xform, coordinates);

        // decides direction the casing ejects and only when not cycling
        if (angle != null)
        {
            var ejectAngle = angle.Value;
            ejectAngle += 3.7f; // 212 degrees; casings should eject slightly to the right and behind of a gun
            ThrowingSystem.TryThrow(entity, ejectAngle.ToVec().Normalized() / 100, 5f);
        }
        if (playSound && TryComp<CartridgeAmmoComponent>(entity, out var cartridge))
        {
            Audio.PlayPredicted(cartridge.EjectSound, entity, user, AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation).WithVolume(-1f));
        }
    }

    public IShootable EnsureShootable(EntityUid uid)
    {
        if (TryComp<CartridgeAmmoComponent>(uid, out var cartridge))
            return cartridge;

        if (TryComp<HitscanAmmoComponent>(uid, out var hitscanAmmo))
            return hitscanAmmo;

        return EnsureComp<AmmoComponent>(uid);
    }

    protected void RemoveShootable(EntityUid uid)
    {
        RemCompDeferred<CartridgeAmmoComponent>(uid);
        RemCompDeferred<AmmoComponent>(uid);
    }

    public void MuzzleFlash(EntityUid gun, AmmoComponent component, Angle worldAngle, EntityUid? user = null) // Trauma - made public
    {
        var attemptEv = new GunMuzzleFlashAttemptEvent();
        RaiseLocalEvent(gun, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        var sprite = component.MuzzleFlash;

        if (sprite == null)
            return;

        var ev = new MuzzleFlashEvent(GetNetEntity(gun), sprite, worldAngle);
        CreateEffect(gun, ev, user);
    }

    public void CauseImpulse(EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, Entity<PhysicsComponent> user)
    {
        var fromMap = TransformSystem.ToMapCoordinates(fromCoordinates).Position;
        var toMap = TransformSystem.ToMapCoordinates(toCoordinates).Position;
        var shotDirection = (toMap - fromMap).Normalized();

        const float impulseStrength = 25.0f;
        var impulseVector = shotDirection * impulseStrength;
        Physics.ApplyLinearImpulse(user, -impulseVector, body: user.Comp);
    }

    public void RefreshModifiers(Entity<GunComponent?> gun, EntityUid? User = null) // GoobStation change - User for NoWieldNeeded
    {
        if (!Resolve(gun, ref gun.Comp))
            return;

        var comp = gun.Comp;
        var ev = new GunRefreshModifiersEvent(
            (gun, comp),
            comp.SoundGunshot,
            comp.CameraRecoilScalar,
            comp.AngleIncrease,
            comp.AngleDecay,
            comp.MaxAngle,
            comp.MinAngle,
            comp.ShotsPerBurst,
            comp.FireRate,
            comp.ProjectileSpeed,
            comp.BurstFireRate, // Goobstation
            comp.BurstCooldown, // Goobstation
            User // GoobStation change - User for NoWieldNeeded
        );

        RaiseLocalEvent(gun, ref ev);

        if (comp.SoundGunshotModified != ev.SoundGunshot)
        {
            comp.SoundGunshotModified = ev.SoundGunshot;
            DirtyField(gun, nameof(GunComponent.SoundGunshotModified));
        }

        if (!MathHelper.CloseTo(comp.CameraRecoilScalarModified, ev.CameraRecoilScalar))
        {
            comp.CameraRecoilScalarModified = ev.CameraRecoilScalar;
            DirtyField(gun, nameof(GunComponent.CameraRecoilScalarModified));
        }

        if (!comp.AngleIncreaseModified.EqualsApprox(ev.AngleIncrease))
        {
            comp.AngleIncreaseModified = ev.AngleIncrease;
            DirtyField(gun, nameof(GunComponent.AngleIncreaseModified));
        }

        if (!comp.AngleDecayModified.EqualsApprox(ev.AngleDecay))
        {
            comp.AngleDecayModified = ev.AngleDecay;
            DirtyField(gun, nameof(GunComponent.AngleDecayModified));
        }

        if (!comp.MaxAngleModified.EqualsApprox(ev.MaxAngle))
        {
            comp.MaxAngleModified = ev.MaxAngle;
            DirtyField(gun, nameof(GunComponent.MaxAngleModified));
        }

        if (!comp.MinAngleModified.EqualsApprox(ev.MinAngle))
        {
            comp.MinAngleModified = ev.MinAngle;
            DirtyField(gun, nameof(GunComponent.MinAngleModified));
        }

        if (comp.ShotsPerBurstModified != ev.ShotsPerBurst)
        {
            comp.ShotsPerBurstModified = ev.ShotsPerBurst;
            DirtyField(gun, nameof(GunComponent.ShotsPerBurstModified));
        }

        if (!MathHelper.CloseTo(comp.FireRateModified, ev.FireRate))
        {
            comp.FireRateModified = ev.FireRate;
            DirtyField(gun, nameof(GunComponent.FireRateModified));
        }

        if (!MathHelper.CloseTo(comp.ProjectileSpeedModified, ev.ProjectileSpeed))
        {
            comp.ProjectileSpeedModified = ev.ProjectileSpeed;
            DirtyField(gun, nameof(GunComponent.ProjectileSpeedModified));
        }

        if (!MathHelper.CloseTo(comp.BurstFireRateModified, ev.BurstFireRate)) // Goobstation - start
        {
            comp.BurstFireRateModified = ev.BurstFireRate;
            DirtyField(gun, nameof(GunComponent.BurstFireRateModified));
        }

        if (!MathHelper.CloseTo(comp.BurstCooldownModified, ev.BurstCooldown))
        {
            comp.BurstCooldownModified = ev.BurstCooldown;
            DirtyField(gun, nameof(GunComponent.BurstCooldownModified));
        }  // Goobstation - end
    }

     // Goobstation
    public void SetTarget(EntityUid projectile,
        EntityUid? target,
        out TargetedProjectileComponent targeted,
        bool dirty = true)
    {
        targeted = EnsureComp<TargetedProjectileComponent>(projectile);
        targeted.Target = TerminatingOrDeleted(target) ? null : GetNetEntity(target); // Trauma - set to null if deleted, use NetEntity otherwise
        if (dirty)
            Dirty(projectile, targeted);
    }

    public void SetFireRate(GunComponent component, float fireRate) // Goobstation
    {
        component.FireRate = fireRate;
    }

    public void SetUseKey(GunComponent component, bool useKey) // Goobstation
    {
        component.UseKey = useKey;
    }

    public void SetSoundGunshot(GunComponent component, SoundSpecifier? sound) // Goobstation
    {
        component.SoundGunshot = sound;
    }

    public void SetClumsyProof(GunComponent component, bool clumsyProof) // Goobstation
    {
        component.ClumsyProof = clumsyProof;
    }

    protected abstract void CreateEffect(EntityUid gunUid, MuzzleFlashEvent message, EntityUid? user = null);

    /// <summary>
    /// Trauma - made concrete, only played by client.
    /// All callers are predicted so anyone in PVS range would already play it locally anyway.
    /// </summary>
    public void PlayImpactSound(EntityUid otherEntity, DamageSpecifier? modifiedDamage, SoundSpecifier? weaponSound, bool forceWeaponSound)
    {
        if (_netManager.IsServer)
            return;

        DebugTools.Assert(!Deleted(otherEntity), "Impact sound entity was deleted");

        // Like projectiles and melee,
        // 1. Entity specific sound
        // 2. Ammo's sound
        // 3. Nothing
        if (!forceWeaponSound && modifiedDamage != null && modifiedDamage.GetTotal() > 0 && TryComp<RangedDamageSoundComponent>(otherEntity, out var rangedSound))
        {
            var type = SharedMeleeWeaponSystem.GetHighestDamageSound(modifiedDamage, ProtoManager);

            if (type != null && rangedSound.SoundTypes?.TryGetValue(type, out var damageSoundType) == true)
            {
                Audio.PlayLocal(damageSoundType, otherEntity, null, AudioParams.Default.WithVariation(MeleeSoundSystem.DamagePitchVariation));
                return;
            }
            if (type != null && rangedSound.SoundGroups?.TryGetValue(type, out var damageSoundGroup) == true)
            {
                Audio.PlayLocal(damageSoundGroup, otherEntity, null, AudioParams.Default.WithVariation(MeleeSoundSystem.DamagePitchVariation));
                return;
            }
        }

        Audio.PlayLocal(weaponSound, otherEntity, null);
    }

    /// <summary>
    /// Used for animated effects on the client.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class HitscanEvent : EntityEventArgs
    {
        public List<(NetCoordinates coordinates, Angle angle, SpriteSpecifier Sprite, float Distance)> Sprites = [];
    }

    /// <summary>
    /// Get the ammo count for a given EntityUid. Can be a firearm or magazine.
    /// </summary>
    public int GetAmmoCount(EntityUid uid)
    {
        var ammoEv = new GetAmmoCountEvent();
        RaiseLocalEvent(uid, ref ammoEv);
        return ammoEv.Count;
    }

    /// <summary>
    /// Get the ammo capacity for a given EntityUid. Can be a firearm or magazine.
    /// </summary>
    public int GetAmmoCapacity(EntityUid uid)
    {
        var ammoEv = new GetAmmoCountEvent();
        RaiseLocalEvent(uid, ref ammoEv);
        return ammoEv.Capacity;
    }

    public override void Update(float frameTime)
    {
        UpdateBattery(frameTime);
        UpdateBallistic(frameTime);
    }
}

/// <summary>
///     Raised directed on the gun before firing to see if the shot should go through.
/// </summary>
/// <remarks>
///     Handling this in server exclusively will lead to mispredicts.
/// </remarks>
/// <param name="User">The user that attempted to fire this gun.</param>
/// <param name="Cancelled">Set this to true if the shot should be cancelled.</param>
/// <param name="ThrowItems">Set this to true if the ammo shouldn't actually be fired, just thrown.</param>
[ByRefEvent]
public record struct AttemptShootEvent(EntityUid User, string? Message, bool Cancelled = false, bool ThrowItems = false);

/// <summary>
///     Raised directed on the gun after firing.
/// </summary>
/// <param name="User">The user that fired this gun.</param>
[ByRefEvent]
public record struct GunShotEvent(EntityUid User, List<(EntityUid? Uid, IShootable Shootable)> Ammo);

/// <summary>
/// Raised on an entity after firing a gun to see if any components or systems would allow this entity to be pushed
/// by the gun they're firing. If true, GunSystem will create an impulse on our entity.
/// </summary>
[ByRefEvent]
public record struct ShooterImpulseEvent()
{
    public bool Push;
};

public enum EffectLayers : byte
{
    Unshaded,
}

[Serializable, NetSerializable]
public enum AmmoVisuals : byte
{
    Spent,
    AmmoCount,
    AmmoMax,
    HasAmmo, // used for generic visualizers. c# stuff can just check ammocount != 0
    MagLoaded,
    BoltClosed,
}

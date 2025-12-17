using Content.Server._Goobstation.Wizard.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Server.Stunnable;
using Content.Server.Temperature.Systems;
using Content.Server.Damage.Components;
using Content.Goobstation.Common.CCVar;
using Content.Shared._Goobstation.Wizard.Spellblade;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.IgnitionSource;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Rejuvenate;
using Content.Shared.Temperature;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee.Events;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Temperature.Components;
using Content.Trauma.Shared.Chaplain.Components;
using Robust.Server.Audio;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Configuration;
using Content.Goobstation.Shared.Religion;



/// <summary>
/// Adds an action ability that will cause all flammable targets in a radius to ignite, also heals the owner
/// of the component when used.
/// </summary>
namespace Content.Trauma.Server.Chaplain.Systems
{
    public sealed class HolyFlammableSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly FixtureSystem _fixture = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly UseDelaySystem _useDelay = default!;
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SpellbladeSystem _spellblade = default!; // Goobstation
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private EntityQuery<InventoryComponent> _inventoryQuery;
        private EntityQuery<PhysicsComponent> _physicsQuery;

        // This should probably be moved to the component, requires a rewrite, all fires tick at the same time
        private const float UpdateTime = 1f;

        private float _timer;

        private readonly Dictionary<Entity<HolyFlammableComponent>, float> _fireEvents = new();

        private int _addHeatFirestack = 1500;
        public override void Initialize()
        {
            _inventoryQuery = GetEntityQuery<InventoryComponent>();
            _physicsQuery = GetEntityQuery<PhysicsComponent>();

            SubscribeLocalEvent<HolyFlammableComponent, MapInitEvent>(OnMapInit);
            //SubscribeLocalEvent<HolyFlammableComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<HolyFlammableComponent, StartCollideEvent>(OnCollide);
            //SubscribeLocalEvent<HolyFlammableComponent, IsHotEvent>(OnIsHot);
            //SubscribeLocalEvent<HolyFlammableComponent, TileFireEvent>(OnTileFire);
            SubscribeLocalEvent<HolyFlammableComponent, RejuvenateEvent>(OnRejuvenate);
            SubscribeLocalEvent<HolyFlammableComponent, ResistFireAlertEvent>(OnResistFireAlert);
            Subs.SubscribeWithRelay<HolyFlammableComponent, ExtinguishEvent>(OnExtinguishEvent);

            SubscribeLocalEvent<IgniteOnCollideComponent, StartCollideEvent>(IgniteOnCollide);
            SubscribeLocalEvent<IgniteOnCollideComponent, LandEvent>(OnIgniteLand);
            //SubscribeLocalEvent<IgniteOnCollideComponent, ProjectileHitEvent>(OnProjectileHit); // Goobstation

            //SubscribeLocalEvent<HolyIgniteOnMeleeHitComponent, MeleeHitEvent>(OnMeleeHit);

            SubscribeLocalEvent<ExtinguishOnInteractComponent, ActivateInWorldEvent>(OnExtinguishActivateInWorld);

            SubscribeLocalEvent<IgniteOnHolyDamageComponent, DamageChangedEvent>(OnDamageChanged);

            Subs.CVar(_cfg, GoobCVars.FireStackHeat, value => _addHeatFirestack = value, true);
        }

        private void OnExtinguishEvent(Entity<HolyFlammableComponent> ent, ref ExtinguishEvent args)
        {
            // You know I'm really not sure if having AdjustFireStacks *after* Extinguish,
            // but I'm just moving this code, not questioning it.
            HolyExtinguish(ent, ent.Comp);
            AdjustFireStacks(ent, args.FireStacksAdjustment, ent.Comp);
        }

        private void OnMeleeHit(EntityUid uid, HolyIgniteOnMeleeHitComponent component, MeleeHitEvent args)
        {
            foreach (var entity in args.HitEntities)
            {
                if (!TryComp<WeakToHolyComponent>(uid, out var weakToHoly))
                    continue;

                if (!EnsureComp<HolyFlammableComponent>(uid, out var flammable))
                    continue;

                AdjustFireStacks(entity, component.FireStacks, flammable);
                if (component.FireStacks >= 0)
                    HolyIgnite(entity, args.Weapon, args.User);
            }
        }

        private void OnIgniteLand(EntityUid uid, IgniteOnCollideComponent component, ref LandEvent args)
        {
            RemCompDeferred<IgniteOnCollideComponent>(uid);
        }

        private void OnProjectileHit(Entity<IgniteOnCollideComponent> ent, ref ProjectileHitEvent args) // Goobstation
        {
            var otherEnt = args.Target;

            if (!TryComp<WeakToHolyComponent>(otherEnt, out var weakToHoly))
                return;

            if (!EnsureComp<HolyFlammableComponent>(otherEnt, out var flammable))
                return;

            flammable.FireStacks += ent.Comp.FireStacks;
            HolyIgnite(otherEnt, ent);
            ent.Comp.Count--;

            if (ent.Comp.Count == 0)
                RemCompDeferred<IgniteOnCollideComponent>(ent);
        }

        private void IgniteOnCollide(EntityUid uid, IgniteOnCollideComponent component, ref StartCollideEvent args)
        {
            if (args.OurFixtureId == SharedProjectileSystem.ProjectileFixture) // Goobstation
                return;

            if (!args.OtherFixture.Hard || component.Count == 0)
                return;

            var otherEnt = args.OtherEntity;

            if (!TryComp(otherEnt, out HolyFlammableComponent? flammable))
                return;

            //Only ignite when the colliding fixture is projectile or ignition.
            if (args.OurFixtureId != component.FixtureId) // Goob edit
            {
                return;
            }

            flammable.FireStacks += component.FireStacks;
            HolyIgnite(otherEnt, uid);
            component.Count--;

            if (component.Count == 0)
                RemCompDeferred<IgniteOnCollideComponent>(uid);
        }

        private void OnMapInit(EntityUid uid, HolyFlammableComponent component, MapInitEvent args)
        {
            // Sets up a fixture for flammable collisions.
            // TODO: Should this be generalized into a general non-hard 'effects' fixture or something? I can't think of other use cases for it.
            // This doesn't seem great either (lots more collisions generated) but there isn't a better way to solve it either that I can think of.

            if (!TryComp<PhysicsComponent>(uid, out var body))
                return;

            _fixture.TryCreateFixture(uid, component.FlammableCollisionShape, component.FlammableFixtureID, hard: false,
                collisionMask: (int) CollisionGroup.FullTileLayer, body: body);
        }

        private void OnInteractUsing(EntityUid uid, HolyFlammableComponent flammable, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(args.Used, isHotEvent);

            if (!isHotEvent.IsHot)
                return;

            HolyIgnite(uid, args.Used, args.User);
            args.Handled = true;
        }

        private void OnExtinguishActivateInWorld(EntityUid uid, ExtinguishOnInteractComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled || !args.Complex)
                return;

            if (!TryComp(uid, out HolyFlammableComponent? flammable))
                return;

            if (!flammable.OnFire)
                return;

            args.Handled = true;

            if (!TryComp(uid, out UseDelayComponent? useDelay) || !_useDelay.TryResetDelay((uid, useDelay), true))
                return;

            _audio.PlayPvs(component.ExtinguishAttemptSound, uid);

            if (_random.Prob(component.Probability))
            {
                AdjustFireStacks(uid, component.StackDelta, flammable);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString(component.ExtinguishFailed), uid);
            }
        }

        private void OnCollide(EntityUid uid, HolyFlammableComponent flammable, ref StartCollideEvent args)
        {
            var otherUid = args.OtherEntity;

            // Collisions cause events to get raised directed at both entities. We only want to handle this collision
            // once, hence the uid check.
            if (otherUid.Id < uid.Id)
                return;

            // Normal hard collisions, though this isn't generally possible since most flammable things are mobs
            // which don't collide with one another, shouldn't work here.
            if (args.OtherFixtureId != flammable.FlammableFixtureID && args.OurFixtureId != flammable.FlammableFixtureID)
                return;

            if (!flammable.FireSpread)
                return;

            if (!TryComp(otherUid, out HolyFlammableComponent? otherFlammable) || !otherFlammable.FireSpread)
                return;

            if (!flammable.OnFire && !otherFlammable.OnFire)
                return; // Neither are on fire

            // Both are on fire -> equalize fire stacks.
            // Weight each thing's firestacks by its mass
            var mass1 = 1f;
            var mass2 = 1f;
            if (_physicsQuery.TryComp(uid, out var physics) && _physicsQuery.TryComp(otherUid, out var otherPhys))
            {
                mass1 = physics.Mass;
                mass2 = otherPhys.Mass;
            }

            // Get the average of both entity's firestacks * mass
            // Then for each entity, we divide the average by their mass and set their firestacks to that value
            // An entity with a higher mass will lose some fire and transfer it to the one with lower mass.
            var avg = (flammable.FireStacks * mass1 + otherFlammable.FireStacks * mass2) / 2f;

            // bring each entity to the same firestack mass, firestack amount is scaled by the inverse of the entity's mass
            SetFireStacks(uid, avg / mass1, flammable, ignite: true);
            SetFireStacks(otherUid, avg / mass2, otherFlammable, ignite: true);
        }

        private void OnIsHot(EntityUid uid, HolyFlammableComponent flammable, IsHotEvent args)
        {
            args.IsHot = flammable.OnFire;
        }

        private void OnTileFire(Entity<HolyFlammableComponent> ent, ref TileFireEvent args)
        {
            var tempDelta = args.Temperature - ent.Comp.MinIgnitionTemperature;

            _fireEvents.TryGetValue(ent, out var maxTemp);

            if (tempDelta > maxTemp)
                _fireEvents[ent] = tempDelta;
        }

        private void OnRejuvenate(EntityUid uid, HolyFlammableComponent component, RejuvenateEvent args)
        {
            HolyExtinguish(uid, component);
        }

        private void OnResistFireAlert(Entity<HolyFlammableComponent> ent, ref ResistFireAlertEvent args)
        {
            if (args.Handled)
                return;

            Resist(ent, ent);
            args.Handled = true;
        }

        public void UpdateAppearance(EntityUid uid, HolyFlammableComponent? flammable = null, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref flammable, ref appearance))
                return;

            _appearance.SetData(uid, FireVisuals.OnFire, flammable.OnFire, appearance);
            _appearance.SetData(uid, FireVisuals.FireStacks, flammable.FireStacks, appearance);

            // Also enable toggleable-light visuals
            // This is intended so that matches & candles can re-use code for un-shaded layers on in-hand sprites.
            // However, this could cause conflicts if something is ACTUALLY both a toggleable light and flammable.
            // if that ever happens, then fire visuals will need to implement their own in-hand sprite management.
            _appearance.SetData(uid, ToggleableVisuals.Enabled, flammable.OnFire, appearance);
        }

        public void AdjustFireStacks(EntityUid uid, float relativeFireStacks, HolyFlammableComponent? flammable = null, bool ignite = false)
        {
            if (!Resolve(uid, ref flammable))
                return;

            SetFireStacks(uid, flammable.FireStacks + relativeFireStacks, flammable, ignite);
        }

        public void SetFireStacks(EntityUid uid, float stacks, HolyFlammableComponent? flammable = null, bool ignite = false)
        {
            if (!Resolve(uid, ref flammable))
                return;

            flammable.FireStacks = MathF.Min(MathF.Max(flammable.MinimumFireStacks, stacks), flammable.MaximumFireStacks);

            // Goobstation modified - fix
            if (flammable.FireStacks <= 0)
                HolyExtinguish(uid, flammable);
            else if (ignite)
                HolyIgnite(uid, null);
        }

        public void HolyExtinguish(EntityUid uid, HolyFlammableComponent? flammable = null)
        {
            // Goobstation - from EE at 7b0949568d07df81b298251c6fce9be4d7d03f18 (https://github.com/Simple-Station/Einstein-Engines/pull/2462)
            if (!Resolve(uid, ref flammable) || !flammable.CanExtinguish)
                return;

            // Goobstation - from EE at 7b0949568d07df81b298251c6fce9be4d7d03f18 (https://github.com/Simple-Station/Einstein-Engines/pull/2462)
            RemCompDeferred<OnHolyFireComponent>(uid);
            if (!flammable.OnFire)
                return;

            _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):entity} stopped being on holy fire damage");
            flammable.OnFire = false;
            flammable.FireStacks = 0;
            flammable.IgnoreFireProtection = false; // EE Plasmamen Change

            var extinguished = new ExtinguishedEvent();
            RaiseLocalEvent(uid, ref extinguished);

            UpdateAppearance(uid, flammable);
            _alertsSystem.ClearAlert(uid, flammable.FireAlert); // Goob Edit - Fix Fire Alert
        }

        // Goobstation - now nullable
        public void HolyIgnite(EntityUid uid, EntityUid? ignitionSource = null, EntityUid? ignitionSourceUser = null, bool ignoreFireProtection = false) // EE Plasmamen Change
        {
            if (!TryComp<WeakToHolyComponent>(uid, out var weakToHoly))
                return;

            if (!EnsureComp<HolyFlammableComponent>(uid, out var flammable))
                return;

            // Goobstation - from EE at 7b0949568d07df81b298251c6fce9be4d7d03f18 (https://github.com/Simple-Station/Einstein-Engines/pull/2462)
            EnsureComp<OnHolyFireComponent>(uid);
            if (flammable.AlwaysCombustible)
            {
                flammable.FireStacks = Math.Max(flammable.FirestacksOnIgnite, flammable.FireStacks);
            }

            if (flammable.FireStacks > 0 && !flammable.OnFire)
            {
                if (ignitionSourceUser != null)
                    _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):target} set on holy fire by {ToPrettyString(ignitionSourceUser.Value):actor} with {ToPrettyString(ignitionSource):tool}");
                else if (ignitionSource != null) // Goobstation
                    _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):target} set on holy fire by {ToPrettyString(ignitionSource):actor}");
                flammable.OnFire = true;

                var extinguished = new IgnitedEvent();
                RaiseLocalEvent(uid, ref extinguished);
            }

            if (ignoreFireProtection) // EE Plasmamen Change
                flammable.IgnoreFireProtection = ignoreFireProtection;

            UpdateAppearance(uid, flammable);
        }

        private void OnDamageChanged(EntityUid uid, IgniteOnHolyDamageComponent component, DamageChangedEvent args)
        {
            // Make sure the entity is flammable
            if (!TryComp<HolyFlammableComponent>(uid, out var flammable))
                return;

            // Make sure the damage delta isn't null
            if (args.DamageDelta == null)
                return;

            // Check if its' taken any heat damage, and give the value
            if (args.DamageDelta.DamageDict.TryGetValue("Holy", out var value))
            {
                // Make sure the value is greater than the threshold
                if (value <= component.Threshold)
                    return;

                // Ignite that sucker
                flammable.FireStacks += component.FireStacks;
                HolyIgnite(uid, uid);
            }


        }

        public void Resist(EntityUid uid,
            HolyFlammableComponent? flammable = null)
        {
            if (!Resolve(uid, ref flammable))
                return;

            if (!flammable.OnFire || !_actionBlockerSystem.CanInteract(uid, null) || flammable.Resisting)
                return;

            flammable.Resisting = true;

            _popup.PopupEntity(Loc.GetString("flammable-component-resist-message"), uid, uid);
            _stunSystem.TryUpdateParalyzeDuration(uid, TimeSpan.FromSeconds(2f));

            // TODO FLAMMABLE: Make this not use TimerComponent...
            uid.SpawnTimer(2000, () =>
            {
                flammable.Resisting = false;
                flammable.FireStacks -= flammable.FirestackFade * 10f; // EE Plasmamen Change
                UpdateAppearance(uid, flammable);
            });
        }

        public override void Update(float frameTime)
        {
            // process all fire events
            foreach (var (flammable, deltaTemp) in _fireEvents)
            {
                // 100 -> 1, 200 -> 2, 400 -> 3...
                var fireStackMod = Math.Max(MathF.Log2(deltaTemp / 100) + 1, 0);
                var fireStackDelta = fireStackMod - flammable.Comp.FireStacks;
                var flammableEntity = flammable.Owner;
                if (fireStackDelta > 0)
                {
                    AdjustFireStacks(flammableEntity, fireStackDelta, flammable);
                }
                HolyIgnite(flammableEntity, flammableEntity);
            }
            _fireEvents.Clear();

            _timer += frameTime;

            if (_timer < UpdateTime)
                return;

            _timer -= UpdateTime;

            // TODO: This needs cleanup to take off the crust from TemperatureComponent and shit.
            // <Goobstation> - from EE at 7b0949568d07df81b298251c6fce9be4d7d03f18 (https://github.com/Simple-Station/Einstein-Engines/pull/2462)
            var query = EntityQueryEnumerator<OnHolyFireComponent>();
            while (query.MoveNext(out var uid, out _))
            {
                if (!TryComp(uid, out HolyFlammableComponent? flammable))
                {
                    RemCompDeferred<OnHolyFireComponent>(uid);
                    continue;
                }
                // </Goobstation>
                if (!flammable.OnFire)
                {
                    // Goobstation - from EE at 7b0949568d07df81b298251c6fce9be4d7d03f18 (https://github.com/Simple-Station/Einstein-Engines/pull/2462)
                    RemCompDeferred<OnHolyFireComponent>(uid);
                    continue;
                }

                if (flammable.FireStacks > 0)
                {

                    _damageableSystem.TryChangeDamage(uid, flammable.Damage * flammable.FireStacks, interruptsDoAfters: false, partMultiplier: 2f); // Lavaland: Nerf fire 
                    AdjustFireStacks(uid, flammable.FirestackFade * (flammable.Resisting ? 10f : 1f), flammable, flammable.OnFire);
                }
                else
                {
                    HolyExtinguish(uid, flammable);
                }
            }
        }
    }
}

using Content.Goobstation.Shared.Religion;
using Content.Server._Goobstation.Wizard.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Stunnable;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
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
using Content.Trauma.Shared.Chaplain;
using Content.Trauma.Shared.Chaplain.Components;
using Microsoft.EntityFrameworkCore.Storage;
using Robust.Server.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;



/// <summary>
/// Adds an action ability that will cause all flammable targets in a radius to ignite, also heals the owner
/// of the component when used.
/// </summary>
namespace Content.Trauma.Server.Chaplain
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
            SubscribeLocalEvent<HolyFlammableComponent, StartCollideEvent>(OnCollide);
            SubscribeLocalEvent<HolyFlammableComponent, TileFireEvent>(OnTileFire);
            SubscribeLocalEvent<HolyFlammableComponent, RejuvenateEvent>(OnRejuvenate);
            SubscribeLocalEvent<HolyFlammableComponent, ResistHolyFireAlertEvent>(OnResistFireAlert);
            Subs.SubscribeWithRelay<HolyFlammableComponent, HolyExtinguishEvent>(OnExtinguishEvent);
            Subs.SubscribeWithRelay<WeakToHolyComponent, HolyIgniteEvent>(OnHolyIgniteEvent);

            SubscribeLocalEvent<HolyIgniteOnCollideComponent, StartCollideEvent>(HolyIgniteOnCollide);
            //SubscribeLocalEvent<IgniteOnCollideComponent, LandEvent>(OnIgniteLand);
            //SubscribeLocalEvent<IgniteOnCollideComponent, ProjectileHitEvent>(OnProjectileHit); // Goobstation

            SubscribeLocalEvent<HolyIgniteOnMeleeHitComponent, MeleeHitEvent>(OnMeleeHit);

            //SubscribeLocalEvent<ExtinguishOnInteractComponent, ActivateInWorldEvent>(OnExtinguishActivateInWorld);

            SubscribeLocalEvent<IgniteOnHolyDamageComponent, DamageChangedEvent>(OnDamageChanged);
        }

        private void OnExtinguishEvent(Entity<HolyFlammableComponent> ent, ref HolyExtinguishEvent args)
        {
            // You know I'm really not sure if having AdjustFireStacks *after* Extinguish,
            // but I'm just moving this code, not questioning it.
            HolyExtinguish(ent, ent.Comp);
            AdjustFireStacks(ent, args.FireStacksAdjustment, ent.Comp);
        }

        private void OnHolyIgniteEvent(Entity<WeakToHolyComponent> ent, ref HolyIgniteEvent args)
        {
            SetupEntity(ent);
            EnsureComp<HolyFlammableComponent>(ent, out var flammable);
            float multiplier = 1f;
            if (flammable.FireStacks > 10)
            {
                multiplier = 0.2f;
            }
            AdjustFireStacks(ent, args.FireStacksAdjustment * multiplier, flammable, true);
        }

        private void OnMeleeHit(EntityUid uid, HolyIgniteOnMeleeHitComponent component, MeleeHitEvent args)
        {
            foreach (var entity in args.HitEntities)
            {
                if (!TryComp<WeakToHolyComponent>(uid, out var weakToHoly))
                    continue;

                SetupEntity(entity);
                EnsureComp<HolyFlammableComponent>(uid, out var flammable);

                AdjustFireStacks(entity, component.FireStacks, flammable, true);
            }
        }

        private void OnIgniteLand(EntityUid uid, HolyIgniteOnCollideComponent component, ref LandEvent args)
        {
            RemCompDeferred<HolyIgniteOnCollideComponent>(uid);
        }

        private void OnProjectileHit(Entity<HolyIgniteOnCollideComponent> ent, ref ProjectileHitEvent args) // Goobstation
        {
            var otherEnt = args.Target;

            if (!TryComp<WeakToHolyComponent>(otherEnt, out var weakToHoly))
                return;

            EnsureComp<HolyFlammableComponent>(otherEnt, out var flammable);

            flammable.FireStacks += ent.Comp.FireStacks;
            HolyIgnite(otherEnt, ent);
            ent.Comp.Count--;

            if (ent.Comp.Count == 0)
                RemCompDeferred<HolyIgniteOnCollideComponent>(ent);
        }

        private void HolyIgniteOnCollide(EntityUid uid, HolyIgniteOnCollideComponent component, ref StartCollideEvent args)
        {
            if (args.OurFixtureId == SharedProjectileSystem.ProjectileFixture) // Goobstation
                return;

            if (!args.OtherFixture.Hard || component.Count == 0)
                return;

            var otherEnt = args.OtherEntity;

            if (!TryComp(otherEnt, out WeakToHolyComponent? weakToHoly))
                return;

            SetupEntity(otherEnt);
            EnsureComp<HolyFlammableComponent>(otherEnt, out var flammable);

            flammable.FireStacks += component.FireStacks;
            HolyIgnite(otherEnt, uid);
            component.Count--;

            if (component.Count == 0)
                RemCompDeferred<HolyIgniteOnCollideComponent>(uid);
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

            
            if (!TryComp<WeakToHolyComponent>(otherUid, out var otherWeak))
                return;

            SetupEntity(otherUid);

            if (!TryComp(otherUid, out HolyFlammableComponent? otherFlammable))
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

        private void OnTileFire(Entity<HolyFlammableComponent> ent, ref TileFireEvent args)
        {
            _fireEvents[ent] = ent.Comp.FireStacks;
        }

        private void OnRejuvenate(EntityUid uid, HolyFlammableComponent component, RejuvenateEvent args)
        {
            HolyExtinguish(uid, component);
        }

        private void OnResistFireAlert(Entity<HolyFlammableComponent> ent, ref ResistHolyFireAlertEvent args)
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

            _appearance.SetData(uid, FireVisuals.OnHolyFire, flammable.OnFire, appearance);
            _appearance.SetData(uid, FireVisuals.HolyFireStacks, flammable.FireStacks, appearance);

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
            EnsureComp<HolyFlammableComponent>(uid, out var flammable);
            EnsureComp<IgniteOnHolyDamageComponent>(uid);

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
                else
                    _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):target} set on holy fire");
                flammable.OnFire = true;

                //var extinguished = new HolyIgnitedEvent();
                //RaiseLocalEvent(uid, ref extinguished);
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

            // Check if its' taken any holy damage, and give the value
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
                flammable.FireStacks -= flammable.FirestackFade * 20f; // EE Plasmamen Change
                UpdateAppearance(uid, flammable);
            });
        }

        public void SetupEntity(EntityUid uid)
        {
            EnsureComp<HolyFlammableComponent>(uid);
            EnsureComp<HolyIgniteOnCollideComponent>(uid);
            EnsureComp<IgniteOnHolyDamageComponent>(uid);
        }
        public float DamageCurve(float x)
        {
            float initialGrowthRate = 0.6f;
            float intermediateGrowthRate = 0.2f;
            float lateGrowthRate = 50.0f;

            if (x < 4)
            {
                return x * initialGrowthRate;
            }
            else if (x >= 4 && x <= 40)
            {
                return initialGrowthRate * 4 + intermediateGrowthRate * (x - 4);
            }
            else
            {
                return initialGrowthRate * 4 + intermediateGrowthRate * (40 - 4) + lateGrowthRate + (x - 40);
            }
        }
        public override void Update(float frameTime)
        {
            // process all fire events
            foreach (var (flammable, deltaTemp) in _fireEvents)
            {
                // 100 -> 1, 200 -> 2, 400 -> 3...
                var fireStackMod = deltaTemp;
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

                // Slowly dry ourselves off if wet.
                if (flammable.FireStacks < 0)
                {
                    flammable.FireStacks = MathF.Min(0, flammable.FireStacks + 1);
                }

                if (!flammable.OnFire)
                {
                    _alertsSystem.ClearAlert(uid, flammable.FireAlert);
                    // Goobstation - from EE at 7b0949568d07df81b298251c6fce9be4d7d03f18 (https://github.com/Simple-Station/Einstein-Engines/pull/2462)
                    RemCompDeferred<OnFireComponent>(uid);
                    continue;
                }

                _alertsSystem.ShowAlert(uid, flammable.FireAlert);

                if (flammable.FireStacks > 0)
                {

                    _damageableSystem.TryChangeDamage(uid, flammable.Damage * DamageCurve(flammable.FireStacks), interruptsDoAfters: false, partMultiplier: 2f); // Lavaland: Nerf fire 
                    AdjustFireStacks(uid, (flammable.FireStacks - 5f) / (50f - 5f) + flammable.FirestackFade * (flammable.Resisting ? 20f : 0f), flammable, flammable.OnFire);
                }
                else
                {
                    HolyExtinguish(uid, flammable);
                }
            }
        }
    }
}

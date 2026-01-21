using Content.Goobstation.Shared.Religion;
using Content.Server.Administration.Logs;
using Content.Server.Stunnable;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Rejuvenate;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Chaplain;
using Content.Trauma.Shared.Chaplain;
using Content.Trauma.Shared.Chaplain.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Trauma.Server.Chaplain;

/// <summary>
/// This system takes care of entities that can catch holy fire by leveraging if the entity has the weakToHolyComponent.
/// </summary>
public sealed class HolyFlammableSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StunSystem _stun = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;

    private const float InitialGrowthRate = 0.6f;
    private const float IntermediateGrowthRate = 0.2f;
    private const float LateGrowthRate = 50.0f;

    public override void Initialize()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<HolyFlammableComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<HolyFlammableComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<HolyFlammableComponent, ResistHolyFireAlertEvent>(OnResistFireAlert);
        Subs.SubscribeWithRelay<HolyFlammableComponent, HolyExtinguishEvent>(OnExtinguishEvent);
        Subs.SubscribeWithRelay<WeakToHolyComponent, HolyIgniteEvent>(OnHolyIgniteEvent);

        SubscribeLocalEvent<HolyIgniteOnCollideComponent, StartCollideEvent>(HolyIgniteOnCollide);
        SubscribeLocalEvent<HolyIgniteOnMeleeHitComponent, MeleeHitEvent>(OnMeleeHit);
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
        var flammable = EnsureComp<HolyFlammableComponent>(ent);
        float multiplier = 1f;
        if (flammable.FireStacks > flammable.FireStacksDropoff)
        {
            multiplier = 0.2f;
        }
        AdjustFireStacks(ent, args.FireStacksAdjustment * multiplier, flammable, true);
    }

    private void OnMeleeHit(Entity<HolyIgniteOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        foreach (var entity in args.HitEntities)
        {
            if (!HasComp<WeakToHolyComponent>(ent))
                continue;

            SetupEntity(entity);
            var flammable = EnsureComp<HolyFlammableComponent>(ent);

            AdjustFireStacks(entity, ent.Comp.FireStacks, flammable, true);
        }
    }

    private void HolyIgniteOnCollide(EntityUid uid, HolyIgniteOnCollideComponent component, ref StartCollideEvent args)
    {
        if (args.OurFixtureId == SharedProjectileSystem.ProjectileFixture)
            return;

        if (!args.OtherFixture.Hard || component.Count == 0)
            return;

        var otherEnt = args.OtherEntity;

        if (!HasComp<WeakToHolyComponent>(otherEnt))
            return;

        SetupEntity(otherEnt);
        var flammable = EnsureComp<HolyFlammableComponent>(otherEnt);

        flammable.FireStacks += component.FireStacks;
        HolyIgnite(otherEnt, uid);
        component.Count--;

        if (component.Count <= 0)
            RemCompDeferred<HolyIgniteOnCollideComponent>(uid);
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

        _appearance.SetData(uid, HolyFireVisuals.OnFire, flammable.OnFire, appearance);
        _appearance.SetData(uid, HolyFireVisuals.FireStacks, flammable.FireStacks, appearance);

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

        if (flammable.FireStacks <= 0)
            HolyExtinguish(uid, flammable);
        else if (ignite)
            HolyIgnite(uid, null);
    }

    public void HolyExtinguish(EntityUid uid, HolyFlammableComponent? flammable = null)
    {
        if (!Resolve(uid, ref flammable) || !flammable.CanExtinguish)
            return;

        RemCompDeferred<OnHolyFireComponent>(uid);
        if (!flammable.OnFire)
            return;

        _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):entity} stopped being on holy fire damage");
        flammable.OnFire = false;
        flammable.FireStacks = 0;

        var extinguished = new ExtinguishedEvent();
        RaiseLocalEvent(uid, ref extinguished);

        UpdateAppearance(uid, flammable);
        _alerts.ClearAlert(uid, flammable.FireAlert);
    }

    public void HolyIgnite(EntityUid uid, EntityUid? ignitionSource = null, EntityUid? ignitionSourceUser = null, bool ignoreFireProtection = false)
    {
        EnsureComp<HolyFlammableComponent>(uid, out var flammable);
        EnsureComp<IgniteOnHolyDamageComponent>(uid);

        EnsureComp<OnHolyFireComponent>(uid);
        if (flammable.AlwaysCombustible)
        {
            flammable.FireStacks = Math.Max(flammable.FirestacksOnIgnite, flammable.FireStacks);
        }

        if (flammable.FireStacks > 0 && !flammable.OnFire)
        {
            if (ignitionSourceUser != null)
                _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):target} set on holy fire by {ToPrettyString(ignitionSourceUser.Value):actor} with {ToPrettyString(ignitionSource):tool}");
            else if (ignitionSource != null)
                _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):target} set on holy fire by {ToPrettyString(ignitionSource):actor}");
            else
                _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):target} set on holy fire");
            flammable.OnFire = true;

            //var extinguished = new HolyIgnitedEvent();
            //RaiseLocalEvent(uid, ref extinguished);
        }

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

        if (!flammable.OnFire || !_actionBlocker.CanInteract(uid, null) || flammable.Resisting)
            return;

        flammable.Resisting = true;
        flammable.ResistTimer = 2f;
        _popup.PopupEntity(Loc.GetString("flammable-component-resist-message"), uid, uid);
        _stun.TryUpdateParalyzeDuration(uid, TimeSpan.FromSeconds(2f));
    }

    public void SetupEntity(EntityUid uid)
    {
        EnsureComp<HolyFlammableComponent>(uid);
        EnsureComp<HolyIgniteOnCollideComponent>(uid);
        EnsureComp<IgniteOnHolyDamageComponent>(uid);
    }
    public float DamageCurve(HolyFlammableComponent flammable)
    {
        float x = flammable.FireStacks;
        return x switch
        {
            < 4 => x * InitialGrowthRate,
            >= 4 and <= 40 => InitialGrowthRate * 4 + IntermediateGrowthRate * (x - 4),
            _ => InitialGrowthRate * 4 + IntermediateGrowthRate * (40 - 4) + LateGrowthRate + (x - 40),
        };
    }
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<OnHolyFireComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (!TryComp(uid, out HolyFlammableComponent? flammable))
            {
                RemCompDeferred<OnHolyFireComponent>(uid);
                continue;
            }

            // Handle resist timer
            if (flammable.ResistTimer >= 0)
            {
                flammable.ResistTimer -= frameTime;
                if (flammable.ResistTimer < 0)
                {
                    flammable.Resisting = false;
                }
            }

            // This refactors the timer so that Holy Fire doesn't tick every frame but rather at fixed intervals and not all at once.
            flammable.Timer += frameTime;
            if (flammable.Timer < flammable.UpdateTime)
                continue;

            flammable.Timer -= flammable.UpdateTime;

            // Slowly Heat up if when we get negative stacks.
            if (flammable.FireStacks < 0)
            {
                flammable.FireStacks = MathF.Min(0, flammable.FireStacks + 1);
            }

            if (!flammable.OnFire)
            {
                _alerts.ClearAlert(uid, flammable.FireAlert);
                RemCompDeferred<OnFireComponent>(uid);
                continue;
            }

            _alerts.ShowAlert(uid, flammable.FireAlert);
            if (flammable.FireStacks > 0)
            {
                _damageable.TryChangeDamage(uid, flammable.Damage * DamageCurve(flammable), interruptsDoAfters: false, partMultiplier: 2f);
                AdjustFireStacks(uid, (flammable.FireStacks - 5f) / (50f - 5f) + flammable.FirestackFade * (flammable.Resisting ? 20f : 0f), flammable, flammable.OnFire);
            }
            else
            {
                HolyExtinguish(uid, flammable);
            }
        }
    }
}

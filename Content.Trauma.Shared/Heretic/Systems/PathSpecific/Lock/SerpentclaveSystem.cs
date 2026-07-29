// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Physics;
using Content.Goobstation.Common.Singularity;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Singularity.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Trauma.Common.Doors;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Lock;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Lock;

public sealed partial class SerpentclaveSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private LockPortalSystem _portal = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private SharedStationAiSystem _ai = default!;
    [Dependency] private SharedDoorSystem _door = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SerpentclaveComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SerpentclaveComponent, SerpentclaveDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<LockTrappedDoorComponent, BeforeDoorAutoCloseEvent>(OnBeforeAutoClose);
        SubscribeLocalEvent<LockTrappedDoorComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
        SubscribeLocalEvent<LockTrappedDoorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LockTrappedDoorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LockTrappedDoorComponent, ShouldDoorCrushEvent>(OnShouldCrush);
        SubscribeLocalEvent<LockTrappedDoorComponent, DoorOpenedEvent>(OnOpen);
        SubscribeLocalEvent<LockTrappedDoorComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpen);
        SubscribeLocalEvent<LockTrappedDoorComponent, DamageDealtEvent>(OnDamageDealt);
        SubscribeLocalEvent<LockTrappedDoorComponent, GettingInteractedWithAttemptEvent>(OnInteractAttempt);

        SubscribeLocalEvent<ContainmentFieldThrowEvent>(OnFieldThrow);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<LockTrappedDoorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextGrapple)
                continue;

            Grapple((uid, comp));
        }
    }

    private void OnFieldThrow(ref ContainmentFieldThrowEvent args)
    {
        if (HasComp<LockTrappedDoorComponent>(args.Field) && _heretic.IsHereticOrGhoul(args.Entity))
            args.Cancelled = true;
    }

    private void OnInteractAttempt(Entity<LockTrappedDoorComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        if (_heretic.IsHereticOrGhoul(args.Uid))
            return;

        args.Cancelled = true;
        AggroTrappedDoor(ent, args.Uid);
    }

    private void OnBeforeDoorOpen(Entity<LockTrappedDoorComponent> ent, ref BeforeDoorOpenedEvent args)
    {
        if (args.User is { } user && !_mobState.IsAlive(user) && !HasComp<GhostComponent>(user))
            args.Cancel();
    }

    private void OnOpen(Entity<LockTrappedDoorComponent> ent, ref DoorOpenedEvent args)
    {
        if (args.User is not { } user || _heretic.IsHereticOrGhoul(user) || HasComp<StunnedComponent>(user))
            return;

        AggroTrappedDoor(ent, user);
    }

    private void OnDamageDealt(Entity<LockTrappedDoorComponent> ent, ref DamageDealtEvent args)
    {
        if (args.Origin is not { } origin)
            return;

        var total = args.Damage.GetTotal();
        if (total <= 0)
            return;

        ent.Comp.SustainedDamage += total;
        Dirty(ent);

        if (ent.Comp.SustainedDamage > ent.Comp.DeathThreshold)
        {
            _audio.PlayPredicted(ent.Comp.DeathSound, ent, origin);
            _jitter.DoJitter(ent, ent.Comp.JitterTime, false);
            RemCompDeferred(ent, ent.Comp);
            return;
        }

        _audio.PlayPredicted(ent.Comp.DamageSound, ent, origin);
    }

    private void OnShouldCrush(Entity<LockTrappedDoorComponent> ent, ref ShouldDoorCrushEvent args)
    {
        args.ShouldCrush = true;
        args.CrushDelay *= 0.75f;
    }

    private void OnShutdown(Entity<LockTrappedDoorComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        RemCompDeferred<ContainmentFieldComponent>(ent);

        _power.SetNeedsPower(ent, true);
    }

    private void OnStartup(Entity<LockTrappedDoorComponent> ent, ref ComponentStartup args)
    {
        if (TryComp(ent, out StationAiWhitelistComponent? whitelist))
            _ai.SetWhitelistEnabled((ent, whitelist), false);

        _power.SetNeedsPower(ent, false);

        var field = EnsureComp<ContainmentFieldComponent>(ent);
        field.DestroyGarbage = false;
        field.ThrowForce = 10;

        var status = EnsureComp<StatusEffectsComponent>(ent);

        if (!status.AllowedEffects.Contains("Jitter"))
            status.AllowedEffects.Add("Jitter");

        _jitter.DoJitter(ent, ent.Comp.JitterTime, false, status: status);
    }

    private void OnBeforeDoorClosed(Entity<LockTrappedDoorComponent> ent, ref BeforeDoorClosedEvent args)
    {
        if (ent.Comp.GrappleTarget != null || _door.GetColliding(ent).Any(_heretic.IsHereticOrGhoul))
        {
            args.Cancel();
            return;
        }

        args.PerformCollisionCheck = false;
    }

    private void OnBeforeAutoClose(Entity<LockTrappedDoorComponent> ent, ref BeforeDoorAutoCloseEvent args)
    {
        args.Modifier = 0.01f;
    }

    private void OnDoAfter(Entity<SerpentclaveComponent> ent, ref SerpentclaveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (args.Target is not { } target)
            return;

        if (_portal.IsDoorOccupied(target, args.User))
            return;

        var comp = EnsureComp<LockTrappedDoorComponent>(target);

        if (_heretic.IsHereticOrGhoul(args.User))
        {
            _audio.PlayPredicted(comp.AggroSound, target, args.User);
            return;
        }

        AggroTrappedDoor((target, comp), args.User);
    }

    private void OnAfterInteract(Entity<SerpentclaveComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !HasComp<AirlockComponent>(args.Target))
            return;

        var doArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.DoAfterTime,
            new SerpentclaveDoAfterEvent(),
            ent,
            target,
            ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnWeightlessMove = false,
            MultiplyDelay = false,
        };

        if (_doAfter.TryStartDoAfter(doArgs))
            args.Handled = true;
    }

    private void Grapple(Entity<LockTrappedDoorComponent> ent)
    {
        if (ent.Comp.GrappleTarget is not { } target)
            return;

        ent.Comp.GrappleTarget = null;
        Dirty(ent);

        var proj = PredictedSpawnAtPosition(ent.Comp.ProjectileProto, Transform(ent).Coordinates);
        var projPos = _transform.GetWorldPosition(proj);
        var targetPos = _transform.GetWorldPosition(target);

        var dir = (targetPos - projPos).Normalized();

        var joint = EnsureComp<ComplexJointVisualsComponent>(proj);
        joint.Data[GetNetEntity(ent)] =
            new ComplexJointVisualsData("grapple", ent.Comp.JointSprite);

        _gun.ShootProjectile(proj, dir, Vector2.Zero, ent, ent);
        _gun.SetTarget(proj, target, out _);
    }

    private void AggroTrappedDoor(Entity<LockTrappedDoorComponent> ent, EntityUid target)
    {
        if (!TryComp(ent, out DoorComponent? door) || !_mobState.IsAlive(target))
            return;

        if (TryComp(ent, out DoorBoltComponent? bolt))
            _door.SetBoltsDown((ent, bolt), false);

        _audio.PlayPredicted(ent.Comp.AggroSound, ent, target);

        if (door.State == DoorState.Open)
        {
            ent.Comp.GrappleTarget = target;
            Grapple(ent);
            return;
        }

        if (!_door.TryOpen(ent))
            return;

        if (ent.Comp.GrappleTarget != null)
            return;

        // Delay grapple for the door to have time to open first
        ent.Comp.GrappleTarget = target;
        ent.Comp.NextGrapple = _timing.CurTime + ent.Comp.GrappleDelay;
        Dirty(ent);
    }
}

[Serializable, NetSerializable]
public sealed partial class SerpentclaveDoAfterEvent : SimpleDoAfterEvent;

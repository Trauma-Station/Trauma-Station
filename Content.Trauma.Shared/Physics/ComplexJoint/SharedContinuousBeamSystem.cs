// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Physics.ComplexJoint;

public abstract partial class SharedContinuousBeamSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedComplexJointVisualsSystem _joint = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPvsOverrideSystem _pvs = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedCombatModeSystem _combat = default!;

    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedTransformSystem Xform = default!;

    [SubscribeLocalEvent, SubscribeNetworkEvent]
    private void OnGetPosition(LaserBeamEndpointPositionEvent ev, EntitySessionEventArgs args)
    {
        if (!TryGetEntity(ev.Uid, out var uid) || args.SenderSession.AttachedEntity is not { } player)
            return;

        if (!TryComp(uid.Value, out ContinuousBeamGunComponent? gun))
            return;

        var xform = Transform(uid.Value);

        if (xform.MapID != ev.Coordinates.MapId)
            return;

        Entity<ContinuousBeamGunComponent> gunEnt = (uid.Value, gun);

        var pos = ev.Coordinates;
        var ourPos = Xform.GetWorldPosition(xform);

        var dir = pos.Position - ourPos;
        var len = dir.Length();
        var newLen = Math.Clamp(len, gun.MinMaxLaserRange.X, gun.MinMaxLaserRange.Y);
        if (Math.Abs(len - newLen) > 0.01f)
            pos = new MapCoordinates(ourPos + dir * newLen / len, xform.MapID);

        gun.CursorPosition = pos;

        if (ev.ShouldFire && ValidateGun(player, gunEnt) && CanFire(player, gunEnt))
            ShootLaser(player, gunEnt.AsNullable(), xform.Coordinates);
        else if (gun.UserCanFire)
            StopFiring(uid.Value, gun, null);
    }

    public bool CanFire(EntityUid user, [NotNullWhen(true)] out Entity<ContinuousBeamGunComponent>? gun)
    {
        return TryGetGun(user, out gun) && CanFire(user, gun.Value);
    }

    public bool CanFire(EntityUid user, Entity<ContinuousBeamGunComponent> gun)
    {
        return gun.Comp.UserCanFire && _combat.IsInCombatMode(user);
    }

    public EntityUid? ShootLaser(EntityUid user, Entity<ContinuousBeamGunComponent?> gun, EntityCoordinates coords)
    {
        if (!Resolve(gun, ref gun.Comp, false))
            return null;

        if (Exists(gun.Comp.Endpoint))
            return gun.Comp.Endpoint.Value;

        var endpoint = PredictedSpawnAtPosition(null, coords);
        var comp = Factory.GetComponent<LaserBeamEndpointComponent>();
        comp.Gun = gun;
        AddComp(endpoint, comp, true);
        if (gun.Comp.BeamTime > 0f)
            EnsureComp<TimedDespawnComponent>(endpoint).Lifetime = gun.Comp.BeamTime;

        var now = Timing.CurTime;

        gun.Comp.Data.CreationTime = now;
        _joint.CreateJoint(endpoint, gun, gun.Comp.Data);

        // Why server? Because audio system is broken there is no way to stop predicted audio on client apparently
        if (_net.IsServer)
        {
            if (Exists(gun.Comp.BeamSoundEnt))
                _audio.Stop(gun.Comp.BeamSoundEnt.Value);
            var filter = Filter.BroadcastMap(Transform(gun).MapID);
            gun.Comp.BeamSoundEnt = _audio.PlayEntity(gun.Comp.BeamSound, filter, gun, false)?.Entity;
        }

        gun.Comp.Endpoint = endpoint;
        gun.Comp.BeamTimer = now + TimeSpan.FromSeconds(gun.Comp.BeamTime);
        gun.Comp.Shooter = user;
        Dirty(gun);

        return endpoint;
    }

    public bool ResolveBeamEndpointData(EntityUid uid,
        ContinuousBeamGunComponent gun,
        ComplexJointVisualsComponent joint)
    {
        var exists = Exists(gun.Endpoint);
        if (exists && gun.CursorPosition != null)
            return true;

        StopFiring(uid, gun, joint);
        return false;
    }

    public void StopFiring(EntityUid uid, ContinuousBeamGunComponent gun, ComplexJointVisualsComponent? joint)
    {
        _joint.ClearBeamJoints((uid, joint), gun.Data.Id);
        if (Exists(gun.Endpoint))
            PredictedQueueDel(gun.Endpoint.Value);
        gun.Endpoint = null;
        Dirty(uid, gun);
    }

    public (Box2Rotated boxRot, Angle angle, float distToEndpoint, Vector2 Dir, Vector2 offset, Vector2 pos, Vector2
        endpointPos)?
        CalculateBeamDamageData(Entity<ContinuousBeamGunComponent, ComplexJointVisualsComponent> ent)
    {
        if (!ResolveBeamEndpointData(ent, ent, ent))
            return null;

        var xform = Transform(ent);

        var pos = Xform.GetWorldPosition(ent.Comp1.Endpoint!.Value);
        var ourPos = Xform.GetWorldPosition(xform);
        var c = pos - ourPos;

        var cLen = c.Length();

        if (cLen <= 0.01f)
            return null;

        var cNorm = c / cLen;
        var angle = c.ToAngle();

        var offset = cNorm * ent.Comp1.BeamScale;
        var box = new Box2(ourPos + offset + new Vector2(0f, -ent.Comp1.LaserThickness),
            ourPos + offset + new Vector2(cLen, ent.Comp1.LaserThickness));
        var boxRot = new Box2Rotated(box, angle, ourPos + offset);
        return (boxRot, angle, cLen, cNorm, offset, ourPos, pos);
    }

    public bool ValidateGun(EntityUid user, Entity<ContinuousBeamGunComponent> gun)
    {
        return user == gun.Owner || _hands.IsHolding(user, gun);
    }

    public bool TryGetGun(EntityUid user, [NotNullWhen(true)] out Entity<ContinuousBeamGunComponent>? gun)
    {
        gun = null;

        if (_hands.GetActiveItem(user) is { } held &&
            TryComp(held, out ContinuousBeamGunComponent? gunComp))
        {
            gun = (held, gunComp);
            return true;
        }

        if (!TryComp(user, out gunComp))
            return false;

        gun = (user, gunComp);
        return true;
    }

    [SubscribeLocalEvent]
    private void OnAttemptAttack(Entity<ContinuousBeamGunComponent> ent, ref AttemptMeleeEvent args)
    {
        if (Exists(ent.Comp.Endpoint))
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnHandUnequip(Entity<ContinuousBeamGunComponent> ent, ref GotUnequippedHandEvent args)
    {
        StopFiring(ent, ent, null);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<LaserBeamEndpointComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.PvsOverride)
            _pvs.RemoveGlobalOverride(ent);

        if (ent.Comp.Gun is not { } gun)
            return;

        if (ent.Comp.PvsOverride)
            _pvs.RemoveGlobalOverride(gun);

        if (!TryComp(gun, out ContinuousBeamGunComponent? gunComp))
            return;

        if (Exists(gunComp.BeamSoundEnt))
            _audio.Stop(gunComp.BeamSoundEnt.Value);

        var ev = new ContinuousBeamStoppedFiringEvent();
        RaiseLocalEvent(gun, ref ev);
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<LaserBeamEndpointComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.PvsOverride)
            _pvs.AddGlobalOverride(ent);

        if (ent.Comp.Gun is not { } gun)
            return;

        if (ent.Comp.PvsOverride)
            _pvs.AddGlobalOverride(gun);
    }
}

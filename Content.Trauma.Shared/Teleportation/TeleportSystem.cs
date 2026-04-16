// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.BlockTeleport;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.Teleportation;

/// <summary>
/// API for other systems to react to when you teleport something.
/// Also provides nice helper for sounds.
/// Please use this instead of copypasting puller shitcode into everything else.
/// </summary>
public sealed class TeleportSystem : EntitySystem
{
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PullerComponent, TeleportingEvent>(OnPullerTeleporting);
        SubscribeLocalEvent<PullableComponent, TeleportingEvent>(OnPullableTeleporting);
    }

    private void OnPullerTeleporting(Entity<PullerComponent> ent, ref TeleportingEvent args)
    {
        if (ent.Comp.Pulling is {} uid && TryComp<PullableComponent>(uid, out var pullable))
            _pulling.TryStopPull(uid, pullable, user: args.User, ignoreGrab: true);
    }

    private void OnPullableTeleporting(Entity<PullableComponent> ent, ref TeleportingEvent args)
    {
        _pulling.TryStopPull(ent, ent.Comp, user: args.User, ignoreGrab: true);
    }

    #region Public API

    /// <summary>
    /// Returns true if an entity is not prevented from teleporting right now.
    /// </summary>
    public bool CanTeleport(EntityUid uid, bool predicted = true)
    {
        var attemptEv = new TeleportAttemptEvent(predicted);
        RaiseLocalEvent(uid, ref attemptEv);
        return !attemptEv.Cancelled;
    }

    /// <summary>
    /// Teleport an entity somewhere, allowing for other systems to clean up e.g. joints.
    /// Returns true if teleporting succeeded.
    /// </summary>
    public bool Teleport(EntityUid uid, EntityCoordinates coords, EntityUid? user = null, bool predicted = true)
    {
        // let other systems prevent teleporting
        if (!CanTeleport(uid, predicted))
            return false;

        // let other systems clean up, e.g. breaking pulls
        var ev = new TeleportingEvent(user, predicted);
        RaiseLocalEvent(uid, ref ev);

        _transform.SetCoordinates(uid, coords);
        _transform.AttachToGridOrMap(uid);
        return true;
    }

    /// <summary>
    /// Teleport an entity, playing the same predicted sound at both where it was and where it teleported to.
    /// </summary>
    public bool Teleport(EntityUid uid, EntityCoordinates coords, SoundSpecifier? sound, EntityUid? user = null, bool predicted = true)
        => Teleport(uid, coords, sound, sound, user, predicted);

    /// <summary>
    /// Teleport an entity, playing distinct predicted sounds where it was and where it teleported to.
    /// </summary>
    public bool Teleport(EntityUid uid, EntityCoordinates coords, SoundSpecifier? soundIn, SoundSpecifier? soundOut, EntityUid? user = null, bool predicted = true)
    {
        var oldCoords = Transform(uid).Coordinates;
        if (predicted)
            _audio.PlayPredicted(soundOut, oldCoords, user);
        else
            _audio.PlayPvs(soundOut, oldCoords);
        var succ = Teleport(uid, coords, user, predicted);
        if (predicted)
            _audio.PlayPredicted(soundIn, coords, user);
        else
            _audio.PlayPvs(soundIn, coords);
        return succ;
    }

    #endregion
}

/// <summary>
/// Raised on an entity being teleported, before its position is changed.
/// </summary>
[ByRefEvent]
public record struct TeleportingEvent(EntityUid? User, bool Predicted);

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.BlockTeleport;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Trauma.Common.MartialArts;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

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
    [Dependency] private readonly SharedJointSystem _joint = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PullerComponent, TeleportingEvent>(OnPullerTeleporting);
        SubscribeLocalEvent<PullableComponent, TeleportingEvent>(OnPullableTeleporting);
        SubscribeLocalEvent<JointComponent, TeleportingEvent>(OnJointTeleporting);
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

    private void OnJointTeleporting(Entity<JointComponent> ent, ref TeleportingEvent args)
    {
        _joint.ClearJoints(ent, ent.Comp);
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
    /// Will break pulls unless you have <c>pulled</c> set, in which case it will teleport the pulled entity as well.
    /// Returns true if teleporting succeeded.
    /// </summary>
    public bool Teleport(EntityUid uid, EntityCoordinates coords, EntityUid? user = null, bool predicted = true, bool pulled = false)
    {
        EntityUid? pullableEntity = null;
        var stage = GrabStage.No;
        // ignores pull restoring logic unless pulled is set.
        if (pulled && TryComp<PullerComponent>(uid, out var puller))
        {
            stage = puller.GrabStage;
            pullableEntity = puller.Pulling;
        }

        if (!TeleportSingle(uid, coords, user, predicted))
            return false;

        // re-pull if teleporting pulled entity succeeds
        if (pullableEntity is {} pulling &&
            TeleportSingle(pulling, coords, user, predicted))
        {
            _pulling.TryStartPull(uid, pulling, grabStageOverride: stage, force: true);
        }

        return true;
    }

    /// <summary>
    /// Teleports a single entity without the pulled-teleporting logic.
    /// </summary>
    public bool TeleportSingle(EntityUid uid, EntityCoordinates coords, EntityUid? user = null, bool predicted = true)
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
    public bool Teleport(EntityUid uid, EntityCoordinates coords, SoundSpecifier? sound, EntityUid? user = null, bool predicted = true, bool pulled = false)
        => Teleport(uid, coords, sound, sound, user, predicted, pulled);

    /// <summary>
    /// Teleport an entity, playing distinct predicted sounds where it was and where it teleported to.
    /// </summary>
    public bool Teleport(EntityUid uid, EntityCoordinates coords, SoundSpecifier? soundIn, SoundSpecifier? soundOut, EntityUid? user = null, bool predicted = true, bool pulled = false)
    {
        var oldCoords = Transform(uid).Coordinates;
        if (predicted)
            _audio.PlayPredicted(soundOut, oldCoords, user);
        else
            _audio.PlayPvs(soundOut, oldCoords);
        var succ = Teleport(uid, coords, user, predicted, pulled);
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

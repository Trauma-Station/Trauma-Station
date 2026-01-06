// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Chat;
using Content.Shared.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Stunnable;

namespace Content.Trauma.Shared.Mobs;

/// <summary>
/// Handles shared interactions with softcrit mobs.
/// </summary>
public abstract partial class SharedSoftCritSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    /// <summary>
    /// Speed modifier for softcrit mobs, on top of being forced to crawl.
    /// </summary>
    public const float SoftCritSpeed = 0.5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SoftCritMobComponent, ComponentStartup>(RefreshSpeed);
        SubscribeLocalEvent<SoftCritMobComponent, ComponentShutdown>(RefreshSpeed);
        SubscribeLocalEvent<SoftCritMobComponent, AttemptStopPullingEvent>(OnAttemptStopPulling);
        SubscribeLocalEvent<SoftCritMobComponent, SpeechTypeOverrideEvent>(OnSpeechTypeOverride);
        SubscribeLocalEvent<SoftCritMobComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<SoftCritMobComponent, StandUpAttemptEvent>(OnStandUpAttempt);
    }

    private void RefreshSpeed(EntityUid uid, SoftCritMobComponent ent, EntityEventArgs args)
    {
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private void OnAttemptStopPulling(Entity<SoftCritMobComponent> ent, ref AttemptStopPullingEvent args)
    {
        // too weak to resist being pulled away into maints
        if (ent.Owner == args.User)
            args.Cancelled = true;
    }

    private void OnSpeechTypeOverride(Entity<SoftCritMobComponent> ent, ref SpeechTypeOverrideEvent args)
    {
        // too fucked up to speak properly
        if (args.DesiredType == InGameICChatType.Speak)
            args.DesiredType = InGameICChatType.Whisper;
    }

    private void OnRefreshSpeed(Entity<SoftCritMobComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(SoftCritSpeed);
    }

    private void OnStandUpAttempt(Entity<SoftCritMobComponent> ent, ref StandUpAttemptEvent args)
    {
        args.Cancelled = true;
    }
}

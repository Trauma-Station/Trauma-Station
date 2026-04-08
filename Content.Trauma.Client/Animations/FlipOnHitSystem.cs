// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Trauma.Shared.Animations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Animations;

public sealed class FlipOnHitSystem : SharedFlipOnHitSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectContainerComponent, AnimationCompletedEvent>(OnAnimationComplete);

        SubscribeLocalEvent<FlippingStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<FlippingStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
    }

    private void OnApplied(Entity<FlippingStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        PlayAnimation(args.Target);
    }

    private void OnRemoved(Entity<FlippingStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Target))
            return;

        _animation.Stop(args.Target, AnimationKey);
    }

    private void OnAnimationComplete(Entity<StatusEffectContainerComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey)
            return;

        if (!Status.HasEffectComp<FlippingStatusEffectComponent>(ent))
            return;

        PlayAnimation(ent);
    }

    private void PlayAnimation(EntityUid user)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (TerminatingOrDeleted(user))
            return;

        if (_animation.HasRunningAnimation(user, AnimationKey))
            return;

        var baseAngle = Angle.Zero;
        if (TryComp(user, out SpriteComponent? sprite))
            baseAngle = sprite.Rotation;

        var degrees = baseAngle.Degrees;

        var animation = new Animation
        {
            Length = Duration,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees - 10), 0f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees + 180), 0.2f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees + 360), 0.2f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees + 540), 0.2f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees + 720), 0.2f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees + 900), 0.2f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees + 1080), 0.2f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees + 1260), 0.2f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees + 1440), 0.2f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees), 0f)
                    }
                }
            }
        };

        _animation.Play(user, animation, AnimationKey);
    }
}

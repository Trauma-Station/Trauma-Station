// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Animations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Trauma.Client.Animations;

public sealed partial class FlipOnHitSystem : SharedFlipOnHitSystem
{
    [Dependency] private AnimationPlayerSystem _animation = default!;
    [Dependency] private AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimationCompletedEvent>(OnAnimationComplete);

        SubscribeNetworkEvent<FlipOnHitEvent>(ev => PlayAnimation(GetEntity(ev.User)));
        SubscribeNetworkEvent<FlipOnHitStopEvent>(ev => StopAnimation(GetEntity(ev.User)));
    }

    private void OnAnimationComplete(AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey || !args.Finished)
            return;

        if (!Status.HasEffectComp<FlippingStatusEffectComponent>(args.Uid))
        {
            RefreshSpriteRotation(args.Uid);
            return;
        }

        PlayAnimation(args.Uid);
    }

    protected override void StopAnimation(EntityUid user)
    {
        _animation.Stop(user, AnimationKey);
        RefreshSpriteRotation(user);
    }

    private void RefreshSpriteRotation(Entity<SpriteComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        _appearance.OnChangeData(ent, ent.Comp);
    }

    protected override void PlayAnimation(EntityUid user)
    {
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

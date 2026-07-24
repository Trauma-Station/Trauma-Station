// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Animations;
using Content.Client.DamageState;
using Content.Client.Stylesheets.Colorspace;
using Content.Goobstation.Shared.Emoting;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Trauma.Common.Wizard;
using Robust.Client.Animations;
using Robust.Shared.Animations;
using Robust.Shared.Timing;

namespace Content.Goobstation.Client.Emoting;

public sealed partial class AnimatedEmotesSystem : SharedAnimatedEmotesSystem
{
    [Dependency] private AnimationPlayerSystem _anim = default!;
    [Dependency] private CommonRaysSystem _rays = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private BodySystem _body = default!;

    private const int TweakAnimationDurationMs = 1100; // 11 frames * 100ms per frame
    private const int FlexAnimationDurationMs = 200 * 7; // 7 frames * 200ms per frame

    private static readonly Dictionary<HumanoidVisualEmoteLayers, ProtoId<OrganCategoryPrototype>> EmoteOrganDict = new()
    {
        {HumanoidVisualEmoteLayers.Tongue, "Tongue"},
        {HumanoidVisualEmoteLayers.Cry, "Eyes"},
    };

    [SubscribeNetworkEvent]
    public void OnBibleSmite(BibleFartSmiteEvent args)
    {
        EntityUid uid = GetEntity(args.Bible);
        if (!_timing.IsFirstTimePredicted || uid == EntityUid.Invalid)
            return;

        var rays = _rays.DoRays(_transform.GetMapCoordinates(uid),
            Color.LightGoldenrodYellow,
            Color.AntiqueWhite,
            10,
            15,
            minMaxRadius: new Vector2(3f, 6f),
            minMaxEnergy: new Vector2(2f, 4f),
            proto: "EffectRayCharge",
            server: false);

        if (rays == null)
            return;

        var track = EnsureComp<TrackUserComponent>(rays.Value);
        track.User = uid;
    }

    public void PlayEmote(EntityUid uid, Animation anim, string animationKey = "emoteAnimKeyId")
    {
        if (_anim.HasRunningAnimation(uid, animationKey))
            return;

        _anim.Play(uid, anim, animationKey);
    }

    [SubscribeLocalEvent]
    private void OnBlacklistAttempt(Entity<AnimatedEmotesBlacklistComponent> ent, ref AnimationVisualEmoteAttemptEvent args)
    {
        if (!args.Cancelled && (ent.Comp.Blacklist & args.Layer) != 0x0)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnBodyAttempt(Entity<BodyComponent> ent, ref AnimationVisualEmoteAttemptEvent args)
    {
        if (args.Cancelled || !EmoteOrganDict.TryGetValue(args.Layer, out var organ))
            return;

        if (_body.GetOrgan(ent.AsNullable(), organ) == null)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnBloodstreamAttempt(Entity<BloodstreamComponent> ent, ref AnimationVisualEmoteAttemptEvent args)
    {
        if (args.Cancelled || args.Layer is not (HumanoidVisualEmoteLayers.Blush or HumanoidVisualEmoteLayers.Tongue))
            return;

        var color = ent.Comp.BloodReferenceSolution.GetColor(ProtoMan);

        args.ColorOverride = color.NudgeLightness(0.3f);
    }

    [SubscribeLocalEvent]
    private void OnAutoHandleState(Entity<AnimatedEmotesComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ProtoMan.TryIndex(ent.Comp.Emote, out var emote) && emote.Event is { } ev)
            RaiseLocalEvent(ent, ev);
    }

    [SubscribeLocalEvent]
    private void OnVisualEmote(Entity<AnimatedEmotesComponent> ent, ref AnimationVisualEmoteEvent args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite) ||
            !_sprite.TryGetLayer((ent, sprite), args.Layer, out var layer, false) || layer.Visible == args.SetVisible)
            return;

        var ev = new AnimationVisualEmoteAttemptEvent(args.Layer);
        RaiseLocalEvent(ent, ref ev);
        if (ev.Cancelled)
            return;

        if (ev.ColorOverride is { } color)
            _sprite.LayerSetColor(layer, color);

        var a = new Animation
        {
            Length = args.Time,
            AnimationTracks =
            {
                new AnimationTrackShowSpriteLayer
                {
                    LayerKey = args.Layer,
                    KeyFrames =
                    {
                        new AnimationTrackShowSpriteLayer.KeyFrame(args.SetVisible, 0f),
                        new AnimationTrackShowSpriteLayer.KeyFrame(!args.SetVisible, (float) args.Time.TotalSeconds),
                    }
                }
            }
        };
        PlayEmote(ent, a, args.Key);
    }

    [SubscribeLocalEvent]
    private void OnFlip(Entity<AnimatedEmotesComponent> ent, ref AnimationFlipEmoteEvent args)
    {
        var a = new Animation
        {
            Length = TimeSpan.FromMilliseconds(500),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(360), 0.25f),
                    }
                }
            }
        };
        PlayEmote(ent, a);
    }

    [SubscribeLocalEvent]
    private void OnSpin(Entity<AnimatedEmotesComponent> ent, ref AnimationSpinEmoteEvent args)
    {
        var a = new Animation
        {
            Length = TimeSpan.FromMilliseconds(600),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(TransformComponent),
                    Property = nameof(TransformComponent.LocalRotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(0), 0f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(90), 0.075f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), 0.075f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(270), 0.075f),
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, 0.075f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(90), 0.075f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), 0.075f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(270), 0.075f),
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, 0.075f),
                    }
                }
            }
        };
        PlayEmote(ent, a, "emoteAnimSpin");
    }

    [SubscribeLocalEvent]
    private void OnJump(Entity<AnimatedEmotesComponent> ent, ref AnimationJumpEmoteEvent args)
    {
        var a = new Animation
        {
            Length = TimeSpan.FromMilliseconds(250),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0, .35f), 0.125f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0.125f),
                    }
                }
            }
        };
        PlayEmote(ent, a);
    }

    [SubscribeLocalEvent]
    private void OnTweak(Entity<AnimatedEmotesComponent> ent, ref AnimationTweakEmoteEvent args)
    {
        if (ent.Comp.TweakState is not { } tweak)
            return;

        var a = new Animation
        {
            Length = TimeSpan.FromMilliseconds(TweakAnimationDurationMs),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = DamageStateVisualLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId(tweak), 0f)
                    }
                }
            }
        };
        PlayEmote(ent, a);
    }

    [SubscribeLocalEvent]
    private void OnFlex(Entity<AnimatedEmotesComponent> ent, ref AnimationFlexEmoteEvent args)
    {
        if (ent.Comp.FlexState is not { } flex ||
            ent.Comp.FlexDefaultState is not { } defaultState ||
            ent.Comp.FlexDamageState is not { } damage ||
            ent.Comp.FlexDefaultDamageState is not { } defaultDamage)
        {
            return;
        }

        var a = new Animation
        {
            Length = TimeSpan.FromMilliseconds(FlexAnimationDurationMs + 100), // give it time to reset
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = DamageStateVisualLayers.Base,
                    KeyFrames =
                    {
                        // TODO: replace this shitcode with component fields holy shit
                        new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId(flex), 0f),
                        new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId(defaultState), FlexAnimationDurationMs / 1000f)
                    }
                },
                // don't display the glow while flexing
                new AnimationTrackSpriteFlick
                {
                    LayerKey = DamageStateVisualLayers.BaseUnshaded,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId(damage), 0f),
                        new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId(defaultDamage), FlexAnimationDurationMs / 1000f)
                    }
                }
            }
        };
        PlayEmote(ent, a);
    }
}

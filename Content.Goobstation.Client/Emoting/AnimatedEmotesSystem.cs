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

    [SubscribeNetworkEvent]
    private void OnVisualEmote(AnimationVisualEmoteEvent args)
    {
        var ent = GetEntity(args.Entity);

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

    [SubscribeNetworkEvent]
    private void OnFlip(AnimationFlipEmoteEvent args)
    {
        var ent = GetEntity(args.Entity);

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

    [SubscribeNetworkEvent]
    private void OnSpin(AnimationSpinEmoteEvent args)
    {
        var ent = GetEntity(args.Entity);

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

    [SubscribeNetworkEvent]
    private void OnJump(AnimationJumpEmoteEvent args)
    {
        var ent = GetEntity(args.Entity);

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

    [SubscribeNetworkEvent]
    private void OnTweak(AnimationTweakEmoteEvent args)
    {
        var ent = GetEntity(args.Entity);

        if (!TryComp(ent, out AnimatedEmotesComponent? comp))
            return;

        var key = DamageStateVisualLayers.Base;

        if (TryGetStateId(ent, comp.TweakState, key) is not { } stateId)
            return;

        var a = new Animation
        {
            Length = TimeSpan.FromMilliseconds(TweakAnimationDurationMs),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = key,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(stateId, 0f)
                    }
                }
            }
        };
        PlayEmote(ent, a);
    }

    [SubscribeNetworkEvent]
    private void OnFlex(AnimationFlexEmoteEvent args)
    {
        var ent = GetEntity(args.Entity);

        if (!TryComp(ent, out AnimatedEmotesComponent? comp))
            return;

        var damageKey = DamageStateVisualLayers.Base;
        var unshadedKey = DamageStateVisualLayers.BaseUnshaded;

        if (TryGetStateId(ent, comp.FlexState, damageKey) is not { } flexId ||
            TryGetStateId(ent, comp.FlexDefaultState, damageKey) is not { } defaultId ||
            TryGetStateId(ent, comp.FlexDamageState, unshadedKey) is not { } flexDamageId ||
            TryGetStateId(ent, comp.FlexDefaultDamageState, unshadedKey) is not { } defaultDamageId)
            return;

        var a = new Animation
        {
            Length = TimeSpan.FromMilliseconds(FlexAnimationDurationMs + 100), // give it time to reset
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = damageKey,
                    KeyFrames =
                    {
                        // TODO: replace this shitcode with component fields holy shit
                        new AnimationTrackSpriteFlick.KeyFrame(flexId, 0f),
                        new AnimationTrackSpriteFlick.KeyFrame(defaultId, FlexAnimationDurationMs / 1000f)
                    }
                },
                // don't display the glow while flexing
                new AnimationTrackSpriteFlick
                {
                    LayerKey = unshadedKey,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(flexDamageId, 0f),
                        new AnimationTrackSpriteFlick.KeyFrame(defaultDamageId, FlexAnimationDurationMs / 1000f)
                    }
                }
            }
        };
        PlayEmote(ent, a);
    }

    private RSI.StateId? TryGetStateId(EntityUid uid, string? state, Enum key)
    {
        if (state == null)
            return null;

        var stateId = new RSI.StateId(state);

        if (_sprite.LayerGetEffectiveRsi(uid, key, stateId) is { } rsi &&
            rsi.TryGetState(stateId, out _))
            return null;

        return stateId;
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.DamageState;
using Content.Client.Stylesheets.Colorspace;
using Content.Goobstation.Shared.Emoting;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Robust.Client.Animations;
using Robust.Shared.Animations;

namespace Content.Goobstation.Client.Emoting;

public sealed partial class AnimatedEmotesSystem : SharedAnimatedEmotesSystem
{
    [Dependency] private AnimationPlayerSystem _anim = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private BodySystem _body = default!;

    private const int TweakAnimationDurationMs = 1100; // 11 frames * 100ms per frame
    private const int FlexAnimationDurationMs = 200 * 7; // 7 frames * 200ms per frame

    private static readonly Dictionary<HumanoidVisualEmoteLayers, ProtoId<OrganCategoryPrototype>> EmoteOrganDict = new()
    {
        {HumanoidVisualEmoteLayers.Tongue, "Tongue"},
        {HumanoidVisualEmoteLayers.Cry, "Eyes"},
    };

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
        if (!TryGetEntity(args.Entity, out var ent))
            return;

        if (!TryComp(ent.Value, out SpriteComponent? sprite) ||
            !_sprite.TryGetLayer((ent.Value, sprite), args.Layer, out var layer, false) || layer.Visible == args.SetVisible)
            return;

        var ev = new AnimationVisualEmoteAttemptEvent(args.Layer);
        RaiseLocalEvent(ent.Value, ref ev);
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
        PlayEmote(ent.Value, a, args.Key);
    }

    [SubscribeNetworkEvent]
    private void OnFlip(AnimationFlipEmoteEvent args)
    {
        if (!TryGetEntity(args.Entity, out var ent))
            return;

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
        PlayEmote(ent.Value, a);
    }

    [SubscribeNetworkEvent]
    private void OnSpin(AnimationSpinEmoteEvent args)
    {
        if (!TryGetEntity(args.Entity, out var ent))
            return;

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
        PlayEmote(ent.Value, a, "emoteAnimSpin");
    }

    [SubscribeNetworkEvent]
    private void OnJump(AnimationJumpEmoteEvent args)
    {
        if (!TryGetEntity(args.Entity, out var ent))
            return;

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
        PlayEmote(ent.Value, a);
    }

    [SubscribeNetworkEvent]
    private void OnTweak(AnimationTweakEmoteEvent args)
    {
        if (!TryGetEntity(args.Entity, out var ent))
            return;

        if (!TryComp(ent.Value, out AnimatedEmotesComponent? comp) || !TryComp(ent.Value, out SpriteComponent? sprite))
            return;

        if (TryGetStateId(sprite, comp.TweakState) is not { } stateId)
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
                        new AnimationTrackSpriteFlick.KeyFrame(stateId, 0f)
                    }
                }
            }
        };
        PlayEmote(ent.Value, a);
    }

    [SubscribeNetworkEvent]
    private void OnFlex(AnimationFlexEmoteEvent args)
    {
        if (!TryGetEntity(args.Entity, out var ent))
            return;

        if (!TryComp(ent.Value, out AnimatedEmotesComponent? comp) || !TryComp(ent.Value, out SpriteComponent? sprite))
            return;

        if (TryGetStateId(sprite, comp.FlexState) is not { } flexId ||
            TryGetStateId(sprite, comp.FlexDefaultState) is not { } defaultId ||
            TryGetStateId(sprite, comp.FlexDamageState) is not { } flexDamageId ||
            TryGetStateId(sprite, comp.FlexDefaultDamageState) is not { } defaultDamageId)
            return;

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
                        new AnimationTrackSpriteFlick.KeyFrame(flexId, 0f),
                        new AnimationTrackSpriteFlick.KeyFrame(defaultId, FlexAnimationDurationMs / 1000f)
                    }
                },
                // don't display the glow while flexing
                new AnimationTrackSpriteFlick
                {
                    LayerKey = DamageStateVisualLayers.BaseUnshaded,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(flexDamageId, 0f),
                        new AnimationTrackSpriteFlick.KeyFrame(defaultDamageId, FlexAnimationDurationMs / 1000f)
                    }
                }
            }
        };
        PlayEmote(ent.Value, a);
    }

    private RSI.StateId? TryGetStateId(SpriteComponent sprite, string? state)
    {
        if (state == null)
            return null;

        var stateId = new RSI.StateId(state);

        if (sprite.BaseRSI?.TryGetState(stateId, out _) is not true)
            return null;

        return stateId;
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.EntityEffects;

namespace Content.Goobstation.Shared.Emoting;

[Serializable, NetSerializable]
public sealed partial class AnimationFlipEmoteEvent : EntityEffectNetworkEvent;

[Serializable, NetSerializable]
public sealed partial class AnimationSpinEmoteEvent : EntityEffectNetworkEvent;

[Serializable, NetSerializable]
public sealed partial class AnimationJumpEmoteEvent : EntityEffectNetworkEvent;

[Serializable, NetSerializable]
public sealed partial class AnimationTweakEmoteEvent : EntityEffectNetworkEvent;

[Serializable, NetSerializable]
public sealed partial class AnimationFlexEmoteEvent : EntityEffectNetworkEvent;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class AnimationVisualEmoteEvent : EntityEffectNetworkEvent
{
    [DataField(required: true)]
    public HumanoidVisualEmoteLayers Layer;

    [DataField(required: true)]
    public TimeSpan Time;

    [DataField(required: true)]
    public string Key;

    [DataField]
    public bool SetVisible = true;
}

[ByRefEvent]
public record struct AnimationVisualEmoteAttemptEvent(
    HumanoidVisualEmoteLayers Layer,
    bool Cancelled = false,
    Color? ColorOverride = null);

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAnimatedEmotesSystem))]
public sealed partial class AnimatedEmotesComponent : Component
{
    /// <summary>
    /// Optional state for the mouse tweaking emote.
    /// </summary>
    [DataField]
    public string? TweakState;

    #region Flex emote states

    [DataField]
    public string? FlexState;

    [DataField]
    public string? FlexDefaultState;

    [DataField]
    public string? FlexDamageState;

    [DataField]
    public string? FlexDefaultDamageState;

    #endregion
}

[Flags, Serializable, NetSerializable]
public enum HumanoidVisualEmoteLayers : byte
{
    None = 0,
    Sigh = 1 << 0,
    Cry = 1 << 1,
    Blush = 1 << 2,
    Tongue = 1 << 3,
}

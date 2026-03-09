// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Emoting;

[Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
public abstract partial class AnimationEmoteEvent : EntityEventArgs
{
    [DataField]
    public virtual bool CausesVomit { get; set; }
}

[Serializable, NetSerializable]
public sealed partial class AnimationFlipEmoteEvent : AnimationEmoteEvent
{
    public override bool CausesVomit { get; set; } = true;
}

[Serializable, NetSerializable]
public sealed partial class AnimationSpinEmoteEvent : AnimationEmoteEvent
{
    public override bool CausesVomit { get; set; } = true;
}

[Serializable, NetSerializable]
public sealed partial class AnimationJumpEmoteEvent : AnimationEmoteEvent;

[Serializable, NetSerializable]
public sealed partial class AnimationTweakEmoteEvent : AnimationEmoteEvent;

[Serializable, NetSerializable]
public sealed partial class AnimationFlexEmoteEvent : AnimationEmoteEvent;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAnimatedEmotesSystem))]
[AutoGenerateComponentState(true)]
public sealed partial class AnimatedEmotesComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype>? Emote;

    [DataField]
    public EntProtoId VomitStatus = "EmoteVomitCounterStatusEffect";

    [DataField]
    public EntProtoId BlockVomitEmoteStatus = "BlockVomitEmotesStatusEffect";

    [DataField]
    public TimeSpan VomitStatusTime = TimeSpan.FromSeconds(1);

    [DataField]
    public int EmotesToVomit = 5;

    [DataField]
    public TimeSpan BlockVomitStatusTime = TimeSpan.FromSeconds(10);

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

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Emoting;

// use as a template
//[Serializable, NetSerializable, DataDefinition] public sealed partial class AnimationNameEmoteEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition] public sealed partial class AnimationFlipEmoteEvent : EntityEventArgs;
[Serializable, NetSerializable, DataDefinition] public sealed partial class AnimationSpinEmoteEvent : EntityEventArgs;
[Serializable, NetSerializable, DataDefinition] public sealed partial class AnimationJumpEmoteEvent : EntityEventArgs;
[Serializable, NetSerializable, DataDefinition] public sealed partial class AnimationTweakEmoteEvent : EntityEventArgs;
[Serializable, NetSerializable, DataDefinition] public sealed partial class AnimationFlexEmoteEvent : EntityEventArgs;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAnimatedEmotesSystem))]
[AutoGenerateComponentState(true)]
public sealed partial class AnimatedEmotesComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype>? Emote;

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

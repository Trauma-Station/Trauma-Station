// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class AreaMansusGraspComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? ChannelStartTime;

    [DataField]
    public TimeSpan ChannelTime = TimeSpan.FromSeconds(2);

    [DataField]
    public float Slope = 2f;

    [DataField]
    public float MaxRange = 5f;

    [DataField]
    public float MinRange;

    [DataField]
    public Color EffectColor = Color.FromHex("#cc66e6");

    [DataField]
    public EntProtoId VisualEffect = "EldritchFlashEffect";

    [DataField]
    public SoundSpecifier ChannelSound = new SoundPathSpecifier("/Audio/Effects/tesla_consume.ogg");
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class FeastOfOwlsComponent : Component
{
    [DataField]
    public int Reward = 5;

    [ViewVariables]
    public int CurrentStep;

    [DataField]
    public TimeSpan Timer = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan ParalyzeTime = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan JitterStutterTime = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier KnowledgeGainSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/eatfood.ogg");
}

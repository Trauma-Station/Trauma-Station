// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ForestAdmonitionsEntityComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastRevealTime;

    [DataField, AutoNetworkedField]
    public float RevealDuration = 5f;

    [DataField, AutoNetworkedField]
    public float RevealDistance = 2f;

    [DataField, AutoNetworkedField]
    public float RevealDistanceSoft;

    [DataField, AutoNetworkedField]
    public float SelfVisibility = 0.2f;

    [DataField, AutoNetworkedField]
    public float ExamineThreshold = 0.2f;

    [DataField]
    public TimeSpan UpdateTime = TimeSpan.FromMilliseconds(100);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}

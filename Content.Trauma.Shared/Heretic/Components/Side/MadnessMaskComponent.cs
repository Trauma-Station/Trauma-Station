// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class MadnessMaskComponent : Component
{
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(0.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public float MaxRange = 8f;

    [DataField]
    public float DistFearModifier = 2f;

    [DataField]
    public float ViewFearModifier = 3f;

    [DataField]
    public float MaxFear = 5f;
}

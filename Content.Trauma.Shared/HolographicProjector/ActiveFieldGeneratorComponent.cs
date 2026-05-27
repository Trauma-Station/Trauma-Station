// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.HolographicProjector;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveFieldGeneratorComponent : Component
{
    /// <summary>
    /// If a generator is charged and enabled, how often should it try to form a connection?
    /// </summary>
    [DataField]
    public TimeSpan ReconnectTime = TimeSpan.FromSeconds(1);

    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ReconnectTimer;
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Server.Heretic.Components.PathSpecific;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class HereticFlamesComponent : Component
{
    [DataField]
    public EntProtoId FireProto = "HereticFireAA";

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan UpdateTimer = TimeSpan.Zero;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan LifetimeTimer = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateDuration = TimeSpan.FromMilliseconds(200);

    [DataField]
    public TimeSpan LifetimeDuration = TimeSpan.FromSeconds(60);

    [DataField]
    public int RangeIncrease;

    [DataField]
    public int Range = 1;
}

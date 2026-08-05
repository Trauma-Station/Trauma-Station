// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.ClockworkCult.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkCultComponent : Component
{
    [DataField]
    public EntProtoId ConvertAction = "ActionClockworkConvert";

    [DataField]
    public EntityUid? ConvertActionEntity;

    [DataField]
    public TimeSpan ConversionDelay = TimeSpan.FromSeconds(6);
}

// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Medical.Shared.OrganRot;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OrganRotComponent : Component
{
    [DataField]
    public TimeSpan RottingInterval = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan NexRot;

    [DataField]
    public float WontRotBelowTemperature = 273.15f; // 0c
}

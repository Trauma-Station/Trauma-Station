// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Lock;

/// <summary>
/// Item that allows lock heretics to trap doors
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SerpentclaveComponent : Component
{
    [DataField]
    public TimeSpan DoAfterTime = TimeSpan.FromSeconds(1.5);
}

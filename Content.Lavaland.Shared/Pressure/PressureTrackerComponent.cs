// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Lavaland.Shared.Pressure;

/// <summary>
/// Tracks the atmospheric pressure this entity is exposed to and networks it for clients to use.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PressureTrackerComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Pressure;
}

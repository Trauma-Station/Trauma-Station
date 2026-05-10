// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Areas;

/// <summary>
/// Component that detects when you enter/exit an area via <see cref="MoveEvent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class AreaDetectorComponent : Component
{
    /// <summary>
    /// The area we are currently residing in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Area;
}

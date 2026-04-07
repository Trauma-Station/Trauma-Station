// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Areas;

/// <summary>
/// Marker component for all areas, used for area lookup.
/// </summary>
[RegisterComponent, NetworkedComponent]
[EntityCategory("Areas")]
public sealed partial class AreaComponent : Component;

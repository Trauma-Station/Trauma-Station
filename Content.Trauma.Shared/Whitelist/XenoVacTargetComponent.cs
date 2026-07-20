// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Whitelist;

/// <summary>
/// Marker component for mobs that can be sucked up by a xenovac.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class XenoVacTargetComponent : Component;

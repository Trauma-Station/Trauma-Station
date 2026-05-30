// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Circuits;

/// <summary>
/// Marker component added to a circuit inside a powered housing.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveCircuitComponent : Component;

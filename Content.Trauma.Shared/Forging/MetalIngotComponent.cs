// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Forging;

/// <summary>
/// Marker component used by an anvil to find ingots to work with.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MetalIngotComponent : Component;

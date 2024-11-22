// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Repulse;

/// <summary>
/// Repulses whatever collides with this entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RepulseOnCollideComponent : Component;

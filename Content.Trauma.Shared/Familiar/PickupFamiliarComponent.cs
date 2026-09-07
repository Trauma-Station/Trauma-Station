// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Familiar;

/// <summary>
/// Sets this entity's master to the first mob that picks it up.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PickupFamiliarComponent : Component;

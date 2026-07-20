// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.Possession;

/// <summary>
/// Marks an entity as immune to possession attempts.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PossessionImmuneComponent : Component
{
}

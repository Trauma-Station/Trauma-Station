// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.Sandevistan;

/// <summary>
/// Marker component for currently enabled Sandevistan users, optimizing query.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveSandevistanUserComponent : Component;

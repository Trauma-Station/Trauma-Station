// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Atmos;

/// <summary>
/// Marker component added to flammable entities while they are on fire.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OnFireComponent : Component;

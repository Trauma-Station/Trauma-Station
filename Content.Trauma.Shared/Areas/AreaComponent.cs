// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Areas;

/// <summary>
/// Marker component for all areas, used for area lookup.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AreaComponent : Component;

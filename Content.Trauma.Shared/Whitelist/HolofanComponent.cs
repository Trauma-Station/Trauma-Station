// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Whitelist;

/// <summary>
/// Marker component for holofans, used for reclaiming charges of the projector.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HolofanComponent : Component;

// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Medical.Components;

/// <summary>
/// Marker component added to limbs during surgery.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VesselsStitchedComponent : Component;

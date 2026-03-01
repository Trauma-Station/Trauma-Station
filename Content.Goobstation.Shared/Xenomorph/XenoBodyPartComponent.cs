// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Xenomorph;

/// <summary>
/// Used to prevent doing normal surgeries on xeno bodyparts and vice versa.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class XenoBodyPartComponent : Component;

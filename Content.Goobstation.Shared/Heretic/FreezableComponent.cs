// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Heretic;

/// <summary>
/// Added to walls which can be converted to ice walls by aristocrat's way.
/// Obviously should not be added to ice walls.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FreezableWallComponent : Component;

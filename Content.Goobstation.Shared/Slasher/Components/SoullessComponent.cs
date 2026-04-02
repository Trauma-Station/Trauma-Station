// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Slasher.Components;

/// <summary>
/// Applied to entities who have had their soul stolen. Prevents the slasher from stealing from the same person multiple times.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SoullessComponent : Component
{
}

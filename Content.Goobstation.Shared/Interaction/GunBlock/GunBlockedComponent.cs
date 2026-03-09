// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Interaction.GunBlock;

/// <summary>
/// Applied to an entity to block gun usage (shoot attempts are cancelled).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GunBlockedComponent : Component
{
    [DataField]
    public string PopupText = "gun-block";
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Mimery;

[RegisterComponent, NetworkedComponent]
public sealed partial class AdvancedMimeryActionComponent : Component
{
    [DataField]
    public LocId VowBrokenMessage = "advanced-mimery-action-vow-broken-message";
}

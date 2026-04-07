// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Attribute.Components;

/// <summary>
/// Placed on an entity (normally an item) to provide something to check against.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StrengthFeatOpposedComponent
{
    /// <summary>
    /// This is what determines how hard it is to make the strength check. Higher value is harder, Lower is weaker.
    /// </summary>
    [DataField]
    public int Mod = 0;
}

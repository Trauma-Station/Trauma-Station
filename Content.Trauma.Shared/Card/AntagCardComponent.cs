// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Card;

/// <summary>
/// Stick on an ID to tell other systems that the ID belongs to someone
/// who is not allowed to be on the station or would be a threat
/// (i. e.) Syndicates/Prisoners/Visitors
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AntagCardComponent : Component
{
    /// <summary>
    /// Threat level. The higher it is, the higher the priority on security systems.
    /// </summary>
    [DataField]
    public int Threat = 1;
}

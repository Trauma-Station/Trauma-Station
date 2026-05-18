// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Card;

/// <summary>
/// Stick on an ID to tell other systems that the ID belongs to someone
/// who is not allowed to be in general areas of the station or would be a threat
/// (i. e.) Syndicates/Prisoners
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AntagCardComponent : Component;

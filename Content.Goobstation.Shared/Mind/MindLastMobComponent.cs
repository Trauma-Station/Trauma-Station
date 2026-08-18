// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Mind;

[RegisterComponent, NetworkedComponent]
public sealed partial class MindLastMobComponent : Component
{
    /// <summary>
    /// The last mob entity this mind was in.
    /// Can be null.
    /// </summary>
    [DataField]
    public EntityUid? LastMob;
}

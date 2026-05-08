// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Grudges.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GrudgeItemComponent : Component
{
    /// <summary>
    /// Who does this item belong to?
    /// </summary>
    [DataField]
    public EntityUid? Grudgee;
}

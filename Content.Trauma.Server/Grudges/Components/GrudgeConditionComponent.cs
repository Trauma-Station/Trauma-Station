// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Grudges.Components;

[RegisterComponent]
public sealed partial class GrudgeConditionComponent : Component
{
    /// <summary>
    /// Who is the grudgee?
    /// </summary>
    [DataField]
    public EntityUid? Guy;

    /// <summary>
    /// The other grudge
    /// </summary>
    [DataField]
    public EntityUid? Grudge;

    /// <summary>
    /// Is accusing grudge
    /// </summary>
    [DataField]
    public bool IsAccuser;
}

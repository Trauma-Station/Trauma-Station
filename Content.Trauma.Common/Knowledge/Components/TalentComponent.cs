// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Knowledge.Components;

/// <summary>
/// Stores the proficiency and proficiency level.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TalentComponent : Component
{
    /// <summary>
    /// Determines the strength of the talent. Most are one and done but some talents can be bought/developed multiple times.
    /// </summary>
    [DataField]
    public int Level = 0;

    /// <summary>
    /// Can take multiple times?
    /// </summary>
    [DataField]
    public bool Repeat = false;
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Morph;

/// <summary>
/// Added to morphs while they are disguised as something.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MorphDisguiseComponent : Component
{
    /// <summary>
    /// Examine message shown when morphed
    /// </summary>
    [DataField]
    public LocId ExamineMessage = "morph-examine";

    [DataField]
    public int ExaminePriority = 15;
}

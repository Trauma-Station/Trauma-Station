// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Objectives;

/// <summary>
/// Requires that a target at least dies once.
/// Depends on <see cref="TargetObjectiveComponent"/> to function.
/// </summary>

[RegisterComponent]
public sealed partial class TeachALessonConditionComponent : Component
{
    /// <summary>
    /// Checks to see if the target has died
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool HasDied = false;

}

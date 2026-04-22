// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Knowledge.Components;

/// <summary>
/// Stores the proficiency. You either got it, or you don't.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ProficiencyComponent : Component
{
    /// <summary>
    /// Does proficiency apply to X with a certain component. Useful for things like items.
    /// </summary>
    [DataField]
    public ComponentRegistry? Registry;

    /// <summary>
    /// Determines the maluses of using something one is not proficient in.
    /// </summary>
    [DataField]
    public ProficiencySkillLevel Type = ProficiencySkillLevel.Minimal;
}

/// <summary>
/// Determines the maluses of not having the necessary proficiency. Not sure how to use this yet.
/// </summary>
public enum ProficiencySkillLevel : byte
{
    Minimal = 0,
    Low = 1,
    Medium = 2,
    High = 3,
}

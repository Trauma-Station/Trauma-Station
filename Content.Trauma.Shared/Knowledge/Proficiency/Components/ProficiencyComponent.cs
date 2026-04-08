using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Knowledge.Proficiency.Components;

/// <summary>
/// 
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ProficiencyComponent : Component
{
    /// <summary>
    /// Proficiency Level. Determines the maluses of using something one is not proficient in.
    /// </summary>
    [DataField]
    public ProficiencySkillLevel Level = ProficiencySkillLevel.Minimal;
}

public enum ProficiencySkillLevel : byte
{
    Minimal = 0,
    Low = 1,
    Medium = 2,
    High = 3,
}

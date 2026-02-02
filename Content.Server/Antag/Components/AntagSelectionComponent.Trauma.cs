using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag.Components;

/// <summary>
/// Trauma - extra field for AntagSelection rules
/// </summary>
public sealed partial class AntagSelectionComponent
{
    /// <summary>
    /// Whether the round end text should show original entity name or mind character name.
    /// </summary>
    [DataField]
    public bool UseCharacterNames;
}

/// <summary>
/// Trauma - extra fields for antag definitions
/// </summary>
public partial struct AntagSelectionDefinition
{
    /// <summary>
    /// A list of jobs which cannnot roll this antag. | GOOBSTATION
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>>? JobBlacklist;

    /// <summary>
    /// Does this antag role roll before job
    /// </summary>
    [DataField]
    public bool RollBeforeJob = true;

    /// <summary>
    /// Unequip all gear before making antag
    /// </summary>
    [DataField]
    public bool UnequipOldGear;

    /// <summary>
    /// If not null, how much chaos should secret+ consider us to have per-antag.
    /// </summary>
    [DataField]
    public float? ChaosScore = null;
}

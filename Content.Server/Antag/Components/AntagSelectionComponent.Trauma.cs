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

namespace Content.Shared.Prying.Components;

/// <summary>
/// Trauma - crowbars instantly prying doors
/// </summary>
public sealed partial class PryingComponent
{
    /// <summary>
    /// Whether the tool is able to instantly pry unpowered unbolted doors and firelocks
    /// </summary>
    [DataField]
    public bool InstaPry = true;
}

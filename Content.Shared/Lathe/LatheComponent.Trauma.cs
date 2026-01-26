namespace Content.Shared.Lathe

/// <summary>
/// Trauma - fields added to LatheComponent.
/// </summary>
public sealed partial class LatheComponent
{
    /// <summary>
    /// Output to MaterialStorage instead of spawning it.
    /// Used by ore processors.
    /// </summary>
    [DataField]
    public bool OutputToStorage;
}

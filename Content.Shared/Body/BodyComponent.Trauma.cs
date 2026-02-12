namespace Content.Shared.Body;

/// <summary>
/// Trauma - bodies visible on thermal vision by default
/// </summary>
public sealed partial class BodyComponent
{
    /// <summary>
    /// Makes this mob show up through walls with thermal vision.
    /// </summary>
    [DataField]
    public bool ThermalVisibility = true;
}

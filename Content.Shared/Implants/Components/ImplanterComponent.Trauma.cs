namespace Content.Shared.Implants.Components;

public sealed partial class ImplanterComponent
{
    /// <summary>
    /// Prevents this implanter being used to implant anything.
    /// </summary>
    [DataField]
    public bool ExtractOnly;
}

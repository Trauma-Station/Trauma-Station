namespace Content.Server.Cloning.Components;

public sealed partial class BeingClonedComponent
{
    /// <summary>
    /// Trauma - The previous body that this is a clone of.
    /// </summary>
    [DataField]
    public EntityUid? Original;
}

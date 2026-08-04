namespace Content.Shared.Mindshield.Components;

public sealed partial class MindShieldComponent
{
    /// <summary>
    /// Set when trying to mindshield a headrev.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Broken;
}

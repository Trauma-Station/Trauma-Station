namespace Content.Shared.Mindshield.Components;

public sealed partial class MindShieldStatusComponent
{
    /// <summary>
    /// Whether the mindshield is broken by being a headrev.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsBroken;
}

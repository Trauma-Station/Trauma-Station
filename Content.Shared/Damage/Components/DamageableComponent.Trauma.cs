namespace Content.Shared.Damage.Components;

public sealed partial class DamageableComponent
{
    /// <summary>
    /// When damage was last modified at.
    /// </summary>
    [ViewVariables]
    public TimeSpan LastModifiedTime;
}

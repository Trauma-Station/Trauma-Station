namespace Content.Shared.Clothing.Components;

public sealed partial class CursedMaskComponent
{
    /// <summary>
    /// Whether to show the examine text, or not.
    /// </summary>
    [DataField]
    public bool ShowExamine = true;

    /// <summary>
    /// Whether to randomize the <see cref="CurrentState"/> on unequip
    /// </summary>
    [DataField]
    public bool Randomize = true;
}

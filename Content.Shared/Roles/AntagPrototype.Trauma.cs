namespace Content.Shared.Roles;
public sealed partial class AntagPrototype
{
    /// <summary>
    /// The image to be displayed in the end credits, if empty it will be text instead
    /// </summary>
    [DataField("creditImage")]
    public string CreditImage { get; private set; } = "";

    /// <summary>
    /// The color associated with this antag
    /// </summary>
    [DataField("color")]
    public Color Color { get; private set; } = Color.Red;


    /// <summary>
    /// Use this to hide the slop stuff like nuke ops command, med, ect
    /// </summary>
    [DataField("dontShowInCredits")]
    public bool DontShowInCredits { get; private set; } = false;
}

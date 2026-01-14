namespace Content.Trauma.Client.Weapons.Ranged;

[RegisterComponent]
public sealed partial class AmmoSelectorMagazineVisualsComponent : Component
{
    /// <summary>
    /// What RsiState we use.
    /// </summary>
    [DataField]
    public Dictionary<string, string> MagStates = new();

    /// <summary>
    /// How many steps there are
    /// </summary>
    [DataField]
    public int MagSteps;

    /// <summary>
    /// Should we hide when the count is 0
    /// </summary>
    [DataField]
    public bool ZeroVisible;
}

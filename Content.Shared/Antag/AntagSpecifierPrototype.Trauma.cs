namespace Content.Shared.Antag;

public sealed partial class AntagSpecifierPrototype
{
    /// <summary>
    /// If true, unequips old gear when this antag is picked for an existing player.
    /// </summary>
    [DataField]
    public bool UnequipOldGear;
}

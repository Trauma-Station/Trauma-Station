using Content.Shared.EntityEffects;

namespace Content.Shared.Antag;

public sealed partial class AntagSpecifierPrototype
{
    /// <summary>
    /// If true, unequips old gear when this antag is picked for an existing player.
    /// </summary>
    [DataField]
    public bool UnequipOldGear;

    /// <summary>
    /// Effects to apply to the player's mob.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public EntityEffect[]? Effects;
}

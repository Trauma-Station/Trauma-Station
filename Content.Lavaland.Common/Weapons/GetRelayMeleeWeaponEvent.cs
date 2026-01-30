namespace Content.Lavaland.Common.Weapons;

/// <summary>
/// Raised on a held item to change which entity to use for melee attacks.
/// </summary>
[ByRefEvent]
public record struct GetRelayMeleeWeaponEvent(EntityUid? Found = null, bool Handled = false);

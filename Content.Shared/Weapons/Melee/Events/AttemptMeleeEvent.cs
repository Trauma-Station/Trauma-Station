using Content.Shared.Weapons.Melee.Components;
namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised directed on a weapon when attempt a melee attack.
/// </summary>
[ByRefEvent]
// Trauma - Added Weapon, WeaponComponent and AttackEvent
public record struct AttemptMeleeEvent(EntityUid User, EntityUid Weapon, MeleeWeaponComponent WeaponComponent, AttackEvent attack, bool Cancelled = false, string? Message = null);

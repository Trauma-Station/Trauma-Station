using Content.Shared.Damage;

namespace Content.Trauma.Shared.Executions;

/// <summary>
/// Used to take ammo and get the damage of that ammo, used in the gunexecution system.
/// </summary>
/// <param name="Damage"></param>
/// <param name="Delete"></param>
/// <param name="Cancelled"></param>
[ByRefEvent]
public record struct TakeAmmoGetDamageFromProjectileEvent(DamageSpecifier Damage, bool Delete = true, bool Cancelled = false);

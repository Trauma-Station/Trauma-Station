// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Trauma.Shared.Executions;

/// <summary>
/// Event raised on ammo entity during gun executions.
/// Used to control how ammo impacts should be faked.
/// If this event is not <c>Handled</c> it will run projectile hit events.
/// If <c>Failed</c> is set, it will play the gun empty sound and have no recoil.
/// Ammo is assumed to successfully execute by default.
/// </summary>
/// <remarks>
/// Fun fact, normal gun code could work like this as well, using a different event of course.
/// Gods strongest shitcoders try to refactor 1 (one) 5 year old system.
/// </remarks>
[ByRefEvent]
public record struct AmmoImpactEvent(Entity<GunComponent> Weapon, EntityUid Shooter, EntityUid Target, bool Handled = false, bool Failed = false);

// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Trauma.Common.Knowledge;

/// <summary>
/// Raised on an attribute holder to calculate the damage modifier.
/// </summary>
[ByRefEvent]
public record struct GetDamageModifierEvent(int Mod = 0);

/// <summary>
/// Raised on an attribute holder to calculate the defense modifier.
/// </summary>
[ByRefEvent]
public record struct GetDefenseModifierEvent(int Mod = 0);

/// <summary>
/// Raised on an attribute holder to calculate the attack modifier.
/// </summary>
[ByRefEvent]
public record struct GetAttackModifierEvent(int Mod = 0);

/// <summary>
/// Raised on an attribute holder to calculate carry limits.
/// </summary>
[ByRefEvent]
public record struct GetCarryLimitsEvent(int Lift = 0, int Carry = 0, int Drag = 0);

/// <summary>
/// Raised on an attribute holder to calculate strength modifier. Use this whenever trying to do something involving a lot of strength like breaking out of cuffs or fighting against a door's bolts.
/// </summary>
[ByRefEvent]
public record struct GetStrengthFeatEvent(int Mod = 0);

/// <summary>
/// Raised on an attribute holder to calculate agility modifier. Imagine using this to keep balance on a beam, avoiding slipping while running, or do something automatic on reflex.
/// </summary>
[ByRefEvent]
public record struct GetAgilityFeatEvent(int Mod = 0);


/// <summary>
/// Raised on an attribute holder to calculate agility modifier. This guy is really one going to be used to dodge projectiles.
/// </summary>
[ByRefEvent]
public record struct GetDodgeSavingThrowEvent(int Mod = 0);

/// <summary>
/// Raised on an attribute holder to calculate mental modifier. Use this guy whenever something like a conversion happens to see if the player can resist it.
/// </summary>
[ByRefEvent]
public record struct GetMentalSavingThrowEvent(int Mod = 0);

/// <summary>
/// Raised on an attribute holder to calculate physical modifier. This guy should be used to see if the player can resist shit like viruses, physical spells, or chems.
/// </summary>
[ByRefEvent]
public record struct GetPhysicalSavingThrowEvent(int Mod = 0);

/// <summary>
/// Raised on an attribute holder to calculate morale modifier. If you look pretty and have a glib tongue, you might just make someone's day better.
/// </summary>
[ByRefEvent]
public record struct GetMoraleModifierEvent(int Mod = 0);

/// <summary>
/// Raised on an knowledge holder to determine if a contest has suceeded. Default logic should go as if this has succeeded.
/// </summary>
[ByRefEvent]
public record struct SingleContestEvent(int DiceUser = 20, int ModUser = 0, int Threshold = 10, bool IsSkill = false, bool Failed = false, bool CriticallyFailed = false, bool CriticallySucceeded = false);

/// <summary>
/// Raised on an knowledge holder to determine if an opposed contest has suceeded. This call can go against items, whatever. Default logic should go as if this has succeeded.
/// </summary>
[ByRefEvent]
public record struct OpposedContestEvent(EntityUid Opposer, int DiceUser = 20, int ModUser = 0, int DiceOpposed = 20, int ModOpposed = 0, bool Failed = false,
    bool CriticallyFailedUser = false, bool CriticallySucceededUser = false,
    bool CriticallyFailedOpposed = false, bool CriticallySucceededOpposed = false);

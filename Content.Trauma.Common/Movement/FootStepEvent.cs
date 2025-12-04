// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.Movement;

/// <summary>
/// Raised on a mob when it plays the footstep sound.
/// This mean it gets raised more often when sprinting, and isn't raised at all if you don't have footstep sounds.
/// For tile movement mobs, it will happen regardless because it has an actual definitive method of knowing when a tile has been moved to.
/// </summary>
[ByRefEvent]
public readonly record struct FootStepEvent(EntityUid Mob);

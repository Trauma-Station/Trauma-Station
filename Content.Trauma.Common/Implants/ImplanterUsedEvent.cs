// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.Implants;

/// <summary>
/// Raised on the implant after it has been installed via an implanter by a player.
/// Not raised for automatic implants e.g. job specials.
/// </summary>
[ByRefEvent]
public readonly record struct ImplanterUsedEvent(EntityUid User, EntityUid Target, EntityUid Implant);

// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Lavaland.Common.Chasm;

/// <summary>
/// Raised on an entity about to get deleted by falling into a chasm.
/// Lets jaunter cancel it to save you.
/// </summary>
[ByRefEvent]
public record struct BeforeChasmFallingEvent(EntityUid Entity, bool Cancelled = false);

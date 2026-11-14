// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Cloning;

/// <summary>
/// Raised on cloned item after it is successfully cloned and equopped
/// </summary>
[ByRefEvent]
public readonly record struct EquipmentCopiedEvent(EntityUid OriginalItem, EntityUid OriginalEnt, EntityUid CloneEnt, bool InternalContentsUnremoveable);

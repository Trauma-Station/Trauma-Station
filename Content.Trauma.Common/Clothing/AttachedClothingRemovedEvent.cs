// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Clothing;

/// <summary>
/// Raised on a piece of ToggleableClothing like a modsuit when one of its attached clothing items is removed.
/// Attached would be the modsuit control unit.
/// </summary>
[ByRefEvent]
public record struct AttachedClothingRemovedEvent(EntityUid Attached);

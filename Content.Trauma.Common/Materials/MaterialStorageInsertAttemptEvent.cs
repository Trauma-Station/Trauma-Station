// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Materials;

/// <summary>
/// Raised on a material storage to allow preventing inserting a material item into it.
/// Handling this event prevents regular insertion logic, cancelled indicates that inserting failed instead of being handled specially.
/// </summary>
[ByRefEvent]
public record struct MaterialStorageInsertAttemptEvent(EntityUid Item, EntityUid User, bool Handled = false, bool Cancelled = false);

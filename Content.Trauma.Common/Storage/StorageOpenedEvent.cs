// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Storage;

/// <summary>
/// Event raised on a storage entity when its UI is opened by someone.
/// </summary>
[ByRefEvent]
public record struct StorageOpenedEvent(EntityUid User);

// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Storage;

/// <summary>
/// Event raised on the user when a storage's UI is closed.
/// </summary>
[ByRefEvent]
public record struct StorageClosedEvent(EntityUid Target);

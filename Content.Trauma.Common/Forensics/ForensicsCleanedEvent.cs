// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Forensics;

/// <summary>
/// Raised on an entity that is being cleaned of evidence.
/// </summary>
[ByRefEvent]
public record struct ForensicsCleanedEvent();

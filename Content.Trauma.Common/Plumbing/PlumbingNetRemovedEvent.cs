// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Plumbing;

/// <summary>
/// Broadcasted when a plumbing network is removed.
/// </summary>
[ByRefEvent]
public record struct PlumbingNetRemovedEvent(EntityUid? Grid, int NetId);

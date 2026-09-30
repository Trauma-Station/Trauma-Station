// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Funeral;

/// <summary>
/// Triggers when a crematorium burns something
/// </summary>

[ByRefEvent]
public record struct CremationOutputEvent(
    EntityUid Crematorium,
    IReadOnlyList<EntityUid> Contents,
    EntProtoId OutputPrototype);

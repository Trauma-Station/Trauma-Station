// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Funeral;

[ByRefEvent]
public readonly record struct CremationOutputEvent(
    EntityUid Crematorium,
    IReadOnlyList<EntityUid> Contents,
    EntProtoId OutputPrototype);

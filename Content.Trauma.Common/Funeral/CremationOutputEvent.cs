// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Funeral;

public sealed class CremationOutputEvent : EntityEventArgs
{
    public readonly EntityUid Crematorium;
    public readonly IReadOnlyList<EntityUid> Contents;

    public EntProtoId OutputPrototype;

    public CremationOutputEvent(
        EntityUid crematorium,
        IReadOnlyList<EntityUid> contents,
        EntProtoId outputPrototype)
    {
        Crematorium = crematorium;
        Contents = contents;
        OutputPrototype = outputPrototype;
    }
}

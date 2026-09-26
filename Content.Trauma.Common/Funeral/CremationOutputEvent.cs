// SPDX-License-Identifier: AGPL-3.0-or-later

/// <summary>
/// Communicates crematorium output check.
/// </summary>
namespace Content.Trauma.Common.Funeral;

[ByRefEvent]
public struct CremationOutputEvent
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

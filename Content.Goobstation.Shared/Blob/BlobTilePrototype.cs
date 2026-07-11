// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Blob.Components;

namespace Content.Goobstation.Shared.Blob;

[Prototype]
public sealed partial class BlobTilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public int Cost;

    [DataField(required: true)]
    public EntProtoId<BlobTileComponent> Entity;

    [DataField]
    public ProtoId<BlobTilePrototype>? Upgrade;

    [DataField]
    public bool CanChangeChem = true;

    /// <summary>
    /// Prevents placing this tile near existing blob nodes.
    /// </summary>
    [DataField]
    public bool BlockNearNodes;
}

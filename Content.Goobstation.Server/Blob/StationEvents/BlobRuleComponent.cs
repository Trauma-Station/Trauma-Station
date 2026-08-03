// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.StationEvents.Events;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Goobstation.Server.Blob.StationEvents;

[RegisterComponent, Access(typeof(BlobSpawnRule))]
public sealed partial class BlobSpawnRuleComponent : Component
{
    [DataField]
    public List<EntProtoId> CarrierBlobProtos = new()
    {
        "SpawnPointGhostBlobRat"
    };

    [DataField]
    public int PlayersPerCarrierBlob = 30;

    [DataField]
    public int MaxCarrierBlob = 2;
}

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared.Decals.DecalGridComponent;

namespace Content.Shared.Decals;

// Trauma - completely rewrote decals to be entity based
[RegisterComponent]
[Access(typeof(SharedDecalSystem))]
[NetworkedComponent]
public sealed partial class DecalGridComponent : Component
{
    [Access(Other = AccessPermissions.ReadExecute)]
    [DataField]
    public DecalGridChunkCollection ChunkCollection = new(new());

    public sealed class DecalChunk
    {
        public HashSet<Decal> Decals = new();
    }

    [DataRecord]
    public partial record DecalGridChunkCollection(Dictionary<Vector2i, DecalChunk> ChunkCollection);
}

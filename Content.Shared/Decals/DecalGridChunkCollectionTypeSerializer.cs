using System.Globalization;
using System.Linq;
using System.Numerics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;
using static Content.Shared.Decals.DecalGridComponent;

namespace Content.Shared.Decals;

// Trauma - completely rewrote decals to be entity based, new v3 which has no ids. entire file is changed
[TypeSerializer]
public sealed partial class DecalGridChunkCollectionTypeSerializer : ITypeSerializer<DecalGridChunkCollection, MappingDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return serializationManager.ValidateNode<Dictionary<Vector2i, Dictionary<uint, Decal>>>(node, context);
    }

    public DecalGridChunkCollection Read(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<DecalGridChunkCollection>? _ = default)
    {
        node.TryGetValue("version", out var versionNode);
        var version = ((ValueDataNode?) versionNode)?.AsInt() ?? 1;
        Dictionary<Vector2i, DecalChunk> dictionary;

        if (version > 2) // v3 is like v2 but a Sequence instead of Mapping for the decals, no more ids
        {
            var nodes = (SequenceDataNode) node["nodes"];
            dictionary = new Dictionary<Vector2i, DecalChunk>();

            foreach (var dNode in nodes)
            {
                var aNode = (MappingDataNode) dNode;
                var data = serializationManager.Read<DecalData>(aNode["node"], hookCtx, context);
                var deckNodes = (SequenceDataNode) aNode["decals"];
                foreach (var decalData in deckNodes)
                {
                    var coords = serializationManager.Read<Vector2>(decalData, hookCtx, context);

                    var chunkOrigin = SharedMapSystem.GetChunkIndices(coords, SharedDecalSystem.ChunkSize);
                    var chunk = dictionary.GetOrNew(chunkOrigin);
                    var decal = new Decal(coords, data.Id, data.Color, data.Angle, data.ZIndex, data.Cleanable);
                    chunk.Decals.Add(decal);
                }
            }
        }
        else if (version == 2)
        {
            var nodes = (SequenceDataNode) node["nodes"];
            dictionary = new Dictionary<Vector2i, DecalChunk>();

            foreach (var dNode in nodes)
            {
                var aNode = (MappingDataNode) dNode;
                var data = serializationManager.Read<DecalData>(aNode["node"], hookCtx, context);
                var deckNodes = (MappingDataNode) aNode["decals"];

                foreach (var decalData in deckNodes.Values)
                {
                    var coords = serializationManager.Read<Vector2>(decalData, hookCtx, context);

                    var chunkOrigin = SharedMapSystem.GetChunkIndices(coords, SharedDecalSystem.ChunkSize);
                    var chunk = dictionary.GetOrNew(chunkOrigin);
                    var decal = new Decal(coords, data.Id, data.Color, data.Angle, data.ZIndex, data.Cleanable);
                    chunk.Decals.Add(decal);
                }
            }
        }
        else
        {
            throw new Exception("v1 decals are no longer supported");
        }

        return new DecalGridChunkCollection(dictionary);
    }

    public DataNode Write(ISerializationManager serializationManager,
        DecalGridChunkCollection value, IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var decals = new Dictionary<DecalData, List<Vector2>>();

        var allData = new MappingDataNode();
        // Want consistent chunk + decal ordering so diffs aren't mangled
        var nodes = new SequenceDataNode();

        // Build all of the decal data + positions first.
        foreach (var chunk in value.ChunkCollection.Values)
        {
            foreach (var decal in chunk.Decals)
            {
                var data = new DecalData(decal);
                var existing = decals.GetOrNew(data);
                existing.Add(decal.Coordinates);
            }
        }

        foreach (var (data, positions) in decals)
        {
            var lookupNode = new MappingDataNode { { "node", serializationManager.WriteValue(data, alwaysWrite, context) } };
            var decks = new SequenceDataNode();

            positions.Sort((a, b) => a.X.CompareTo(b.X));

            foreach (var pos in positions)
            {
                // Inline coordinates
                decks.Add(serializationManager.WriteValue(pos, alwaysWrite, context));
            }

            lookupNode.Add("decals", decks);
            nodes.Add(lookupNode);
        }

        allData.Add("version", 3.ToString(CultureInfo.InvariantCulture));
        allData.Add("nodes", nodes);

        return allData;
    }

    [DataRecord]
    private readonly partial struct DecalData : IEquatable<DecalData>, IComparable<DecalData>
    {
        public string Id { get; init; } = string.Empty;

        public Color? Color { get; init; }

        public Angle Angle { get; init; } = Angle.Zero;

        public int ZIndex { get; init; }

        public bool Cleanable { get; init; }

        public DecalData(string id, Color? color, Angle angle, int zIndex, bool cleanable)
        {
            Id = id;
            Color = color;
            Angle = angle;
            ZIndex = zIndex;
            Cleanable = cleanable;
        }

        public DecalData(Decal decal)
        {
            Id = decal.Id;
            Color = decal.Color;
            Angle = decal.Angle;
            ZIndex = decal.ZIndex;
            Cleanable = decal.Cleanable;
        }

        public bool Equals(DecalData other)
        {
            return Id == other.Id &&
                   Nullable.Equals(Color, other.Color) &&
                   Angle.Equals(other.Angle) &&
                   ZIndex == other.ZIndex &&
                   Cleanable == other.Cleanable;
        }

        public override bool Equals(object? obj)
        {
            return obj is DecalData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Color, Angle, ZIndex, Cleanable);
        }

        public int CompareTo(DecalData other)
        {
            var idComparison = string.Compare(Id, other.Id, StringComparison.Ordinal);
            if (idComparison != 0)
                return idComparison;

            var colorComparison = string.Compare(Color?.ToHex(), other.Color?.ToHex(), StringComparison.Ordinal);

            if (colorComparison != 0)
                return colorComparison;

            var angleComparison = Angle.Theta.CompareTo(other.Angle.Theta);

            if (angleComparison != 0)
                return angleComparison;

            var zIndexComparison = ZIndex.CompareTo(other.ZIndex);
            if (zIndexComparison != 0)
                return zIndexComparison;

            return Cleanable.CompareTo(other.Cleanable);
        }
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Materials;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Client.Materials.UI;

/// <summary>
/// Trauma - stuff for oreproc
/// </summary>
public sealed partial class MaterialStorageControl
{
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    private TagSystem _tag = default!;

    public static readonly ProtoId<TagPrototype> Ore = "Ore";

    private void InitializeTrauma()
    {
        _tag = _entityManager.System<TagSystem>();
    }

    private Dictionary<ProtoId<MaterialPrototype>, int> FilterOutOres(Dictionary<ProtoId<MaterialPrototype>, int> materials)
    {
        return materials.Where(pair =>
            !(_proto.TryIndex<MaterialPrototype>(pair.Key, out var proto) &&
            proto.StackEntity != null &&
            _proto.TryIndex(proto.StackEntity, out var entityProto) &&
            entityProto.TryGetComponent<TagComponent>(out var tag, _factory) &&
            _tag.HasTag(tag, Ore)))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
